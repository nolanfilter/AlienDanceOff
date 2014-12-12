using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameAgent : MonoBehaviour {

	public enum Points
	{
		Head = 0,
		Hips = 1,
		Shoulder = 2,
		Invalid = 3,
	}

	public enum Parts
	{
		Tail = 0,
		Eyestalk = 1,
		SleepyFriend = 2,
		Invalid = 3,
	}

	public enum Difficulty
	{
		None = 0,
		Easy = 1,
		Medium = 2,
		Hard = 3,
	}
	private Difficulty currentDifficulty = Difficulty.Easy;

	public enum ScreenType
	{
		Freeze = 0,
		GameOver = 1,
		Googlies = 2,
		ShakeTail = 3,
		Shoulder = 4,
		Sleepy = 5,
		WelcomeTo = 6,
		Invalid = 7,
	}

	public AudioClip[] musicClips;
	public GUIStyle textStyle;
	public SpriteRenderer[] screenSprites = new SpriteRenderer[ Enum.GetNames( typeof( ScreenType ) ).Length - 1 ];

	private AudioSource audioSource = null;

	private bool isDancing = false;
	private bool wasDancing = false;

	private Dictionary<Points, Parts> desiredPointPartPairs = new Dictionary<Points, Parts>();
	private Dictionary<Points, Parts> actualPointPartPairs = new Dictionary<Points, Parts>();
	private List<int>[] pointValues = new List<int>[ Enum.GetNames( typeof( Points ) ).Length - 1 ];
	private int numPoints = Enum.GetNames( typeof( Points ) ).Length - 1;
	private Vector2[] partsFrequencyRanges = new Vector2[ Enum.GetNames( typeof( Parts ) ).Length - 1 ];

	private string displayString = "";

	private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	private string alienBodyPartName = "";

	private int numInputValuesRemembered = 25;

	private bool hasStarted = false;

	private ScreenType nextScreenTypeToDisplay = ScreenType.Invalid;

	private static GameAgent mInstance = null;
	public static GameAgent instance
	{
		get
		{
			return mInstance;
		}
	}

	void Awake()
	{
		if( mInstance != null )
		{
			Debug.LogError( string.Format( "Only one instance of GameAgent allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			Destroy( gameObject );
			return;
		}
		
		mInstance = this;

		for( int i = 0; i < pointValues.Length; i++ )
			pointValues[i] = new List<int>();

		partsFrequencyRanges[ (int)Parts.Tail ] = new Vector2( 0.66f, 1f );
		partsFrequencyRanges[ (int)Parts.Eyestalk ] = new Vector2( 0.33f, 0.66f );
		partsFrequencyRanges[ (int)Parts.SleepyFriend ] = new Vector2( 0f, 0.33f );

		for( int i = 0; i < screenSprites.Length; i++ )
			screenSprites [i].enabled = false;
	}

	void Start()
	{
		if( musicClips.Length > 0 )
		{
			audioSource = Camera.main.gameObject.AddComponent<AudioSource>();

			audioSource.loop = true;
		}

		SetDesiredPointPartPairs();

		screenSprites[ (int)ScreenType.WelcomeTo ].enabled = true;
	}

	void Update()
	{
		string currentInputString = SerialAgent.SerialReadLine();

		for( int i = 0; i < currentInputString.Length; i++ )
			EvaluateInputValue( currentInputString[i] );

		currentInputString = Input.inputString;

		for( int i = 0; i < currentInputString.Length; i++ )
			EvaluateInputValue( currentInputString[i] );

		for( int i = 0; i < pointValues.Length; i++ )
		{
			int inputValuesRememberedDelta = pointValues[i].Count - numInputValuesRemembered;

			if( inputValuesRememberedDelta > 0 )
				pointValues[i].RemoveRange( 0, inputValuesRememberedDelta );
		}

		if( !hasStarted )
		{
			for( int i = 0; i < pointValues.Length; i++ )
			{
				int count = pointValues[i].Count;
				
				float frequency = 0f;
				
				if( count > 0 )
				{
					int numValuesOn = 0;
					
					for( int j = 0; j < count; j++ )
						if( pointValues[i][j] == 1 )
							numValuesOn++;
					
					frequency = (float)numValuesOn / (float)count;
				}
				
				if( frequency > 0.5f )
				{
					hasStarted = true;
					screenSprites[ (int)ScreenType.WelcomeTo ].enabled = false;
				}
			}
			
			return;
		}

		if( Input.GetKeyDown( KeyCode.Space ) )
			SetDesiredPointPartPairs();

		if( Input.GetKeyDown( KeyCode.LeftArrow ) )
		{
			int currentDifficultyInt = (int)currentDifficulty;
			
			if( currentDifficultyInt > 0 )
			{
				currentDifficulty = (Difficulty)(currentDifficultyInt - 1);
				SetDesiredPointPartPairs();
			}
		}

		if( Input.GetKeyDown( KeyCode.RightArrow ) )
		{
			int currentDifficultyInt = (int)currentDifficulty;

			if( currentDifficultyInt < 3 )
			{
				currentDifficulty = (Difficulty)(currentDifficultyInt + 1);
				SetDesiredPointPartPairs();
			}
		}

		displayString = "";

		for( int i = 0; i < (int)currentDifficulty; i++ )
			displayString += "\t";

		displayString += currentDifficulty + "\n\n";

		isDancing = true;

		foreach( KeyValuePair<Points, Parts> kvp in desiredPointPartPairs )
		{
			if( !actualPointPartPairs.ContainsKey( kvp.Key ) || ( actualPointPartPairs.ContainsKey( kvp.Key ) && actualPointPartPairs[ kvp.Key ] != kvp.Value ) )
			{
				isDancing = false;
				displayString += "Connect " + kvp.Value + " to " + kvp.Key + "\n\n";
			}
			else
			{
				displayString += "\t\t" + kvp.Value + " connected to " + kvp.Key + "\n\n";
			}
		}

		if( isDancing && !wasDancing )
		{
			if( audioSource )
				audioSource.Play();

			foreach( KeyValuePair<Points, Parts> kvp in desiredPointPartPairs )
				pointValues[ (int)kvp.Key ].Clear();

			if( nextScreenTypeToDisplay == ScreenType.Googlies )
				StartCoroutine( "DisplayGooglies" );
			else if( nextScreenTypeToDisplay != ScreenType.Invalid )
				screenSprites[ (int)nextScreenTypeToDisplay ].enabled = true;
		}

		if( !isDancing && wasDancing )
		{
			if( audioSource )
				audioSource.Pause();

			for( int i = 0; i < screenSprites.Length; i++ )
				screenSprites [i].enabled = false;
		}

		screenSprites[ (int)ScreenType.Freeze ].enabled = ( currentDifficulty == Difficulty.None );

		if( isDancing )
		{
			if( currentDifficulty == Difficulty.None )
			{
				displayString = "Taking a break.";
			}
			else
			{
				displayString = "\tDANCE YOUR " + alienBodyPartName + " OFF!!!!!!\n\n";

				foreach( KeyValuePair<Points, Parts> kvp in desiredPointPartPairs )
				{
					int count = pointValues[ (int)kvp.Key ].Count;
					
					float frequency = 0f;
					
					if( count > 0 )
					{
						int numValuesOn = 0;
						
						for( int i = 0; i < count; i++ )
							if( pointValues[ (int)kvp.Key ][i] == 1 )
								numValuesOn++;
						
						frequency = (float)numValuesOn / (float)count;
					}
					
					if( frequency < partsFrequencyRanges[ (int)kvp.Value ].x )
						displayString += "" + kvp.Value + " is too slow!\n\n";
					else if( frequency > partsFrequencyRanges[ (int)kvp.Value ].y )
						displayString += "\t\t" + kvp.Value + " is too fast!\n\n";
					else
						displayString += "\t" + kvp.Value + " is good!\n\n";

					if( kvp.Value == Parts.SleepyFriend && frequency > partsFrequencyRanges[ (int)Parts.SleepyFriend ].y )
						StartCoroutine( "DisplayGameOver" );
				}
			}
		}

		wasDancing = isDancing;
	}

	void OnGUI()
	{
		if( screenSprites[ (int)ScreenType.WelcomeTo ].enabled || screenSprites[ (int)ScreenType.Googlies ].enabled || screenSprites[ (int)ScreenType.GameOver ].enabled )
			return;

		GUI.Label( new Rect( 5, 5, 500, 500 ), displayString, textStyle );
	}

	private void EvaluateInputValue( char inputValue )
	{
		switch( inputValue )
		{
			case 'a': if( !actualPointPartPairs.ContainsKey( Points.Head ) ) actualPointPartPairs.Add( Points.Head, Parts.Tail ); break;
			case 'b': if( actualPointPartPairs.ContainsKey( Points.Head ) ) actualPointPartPairs.Remove( Points.Head ); break;
			case 'c': if( !actualPointPartPairs.ContainsKey( Points.Head ) ) actualPointPartPairs.Add( Points.Head, Parts.SleepyFriend ); break;
			case 'd': if( actualPointPartPairs.ContainsKey( Points.Head ) ) actualPointPartPairs.Remove( Points.Head ); break;
			case 'e': if( !actualPointPartPairs.ContainsKey( Points.Head ) ) actualPointPartPairs.Add( Points.Head, Parts.Eyestalk ); break;
			case 'f': if( actualPointPartPairs.ContainsKey( Points.Head ) ) actualPointPartPairs.Remove( Points.Head ); break;
			case 'g': if( !actualPointPartPairs.ContainsKey( Points.Shoulder ) ) actualPointPartPairs.Add( Points.Shoulder, Parts.Tail ); break;
			case 'h': if( actualPointPartPairs.ContainsKey( Points.Shoulder ) ) actualPointPartPairs.Remove( Points.Shoulder ); break;
			case 'i': if( !actualPointPartPairs.ContainsKey( Points.Shoulder ) ) actualPointPartPairs.Add( Points.Shoulder, Parts.SleepyFriend ); break;
			case 'j': if( actualPointPartPairs.ContainsKey( Points.Shoulder ) ) actualPointPartPairs.Remove( Points.Shoulder ); break;
			case 'k': if( !actualPointPartPairs.ContainsKey( Points.Shoulder ) ) actualPointPartPairs.Add( Points.Shoulder, Parts.Eyestalk ); break;
			case 'l': if( actualPointPartPairs.ContainsKey( Points.Shoulder ) ) actualPointPartPairs.Remove( Points.Shoulder ); break;
			case 'm': if( !actualPointPartPairs.ContainsKey( Points.Hips ) ) actualPointPartPairs.Add( Points.Hips, Parts.Tail ); break;
			case 'n': if( actualPointPartPairs.ContainsKey( Points.Hips ) ) actualPointPartPairs.Remove( Points.Hips ); break;
			case 'o': if( !actualPointPartPairs.ContainsKey( Points.Hips ) ) actualPointPartPairs.Add( Points.Hips, Parts.SleepyFriend ); break;
			case 'p': if( actualPointPartPairs.ContainsKey( Points.Hips ) ) actualPointPartPairs.Remove( Points.Hips ); break;
			case 'q': if( !actualPointPartPairs.ContainsKey( Points.Hips ) ) actualPointPartPairs.Add( Points.Hips, Parts.Eyestalk ); break;
			case 'r': if( actualPointPartPairs.ContainsKey( Points.Hips ) ) actualPointPartPairs.Remove( Points.Hips ); break;
			case 's': pointValues[ (int)Points.Head ].Add( 0 ); break;
			case 't': pointValues[ (int)Points.Head ].Add( 1 ); break;
			case 'u': pointValues[ (int)Points.Shoulder ].Add( 0 ); break;
			case 'v': pointValues[ (int)Points.Shoulder ].Add( 1 ); break;
			case 'w': pointValues[ (int)Points.Hips ].Add( 0 ); break;
			case 'x': pointValues[ (int)Points.Hips ].Add( 1 ); break;
		}
	}

	private void SetDesiredPointPartPairs()
	{
		desiredPointPartPairs.Clear();

		List<Points> possiblePoints = new List<Points>();
		
		foreach( Points point in Enum.GetValues( typeof( Points ) ) )
			if( point != Points.Invalid )
				possiblePoints.Add( point );

		List<Parts> possibleParts = new List<Parts>();

		foreach( Parts part in Enum.GetValues( typeof( Parts ) ) )
			if( part != Parts.Invalid )
				possibleParts.Add( part );

		for( int i = 0; i < (int)currentDifficulty; i++ )
		{
			KeyValuePair<Points, Parts> pointPartPair = RandomPointPartPair( possiblePoints, possibleParts );

			desiredPointPartPairs.Add( pointPartPair.Key, pointPartPair.Value );

			possiblePoints.Remove( pointPartPair.Key );
			possibleParts.Remove( pointPartPair.Value );
		}

		alienBodyPartName = "";
		
		for( int i = 0; i < UnityEngine.Random.Range( 5, 12 ); i++ )
			alienBodyPartName += alphabet[ UnityEngine.Random.Range( 0, alphabet.Length ) ];

		if( audioSource )
			audioSource.clip = musicClips[ UnityEngine.Random.Range( 0, musicClips.Length ) ];

		List<ScreenType> possibleScreensToDisplay = new List<ScreenType>();
		
		if( desiredPointPartPairs.ContainsKey( Points.Shoulder ) )
			possibleScreensToDisplay.Add( ScreenType.Shoulder );
		
		if( desiredPointPartPairs.ContainsValue( Parts.Tail ) )
			possibleScreensToDisplay.Add( ScreenType.ShakeTail );
		
		if( desiredPointPartPairs.ContainsValue( Parts.Eyestalk ) )
			possibleScreensToDisplay.Add( ScreenType.Googlies );
		
		if( desiredPointPartPairs.ContainsValue( Parts.SleepyFriend ) )
			possibleScreensToDisplay.Add( ScreenType.Sleepy );

		if( possibleScreensToDisplay.Count > 0 )
			nextScreenTypeToDisplay = possibleScreensToDisplay[ UnityEngine.Random.Range( 0, possibleScreensToDisplay.Count ) ];
		else
			nextScreenTypeToDisplay = ScreenType.Invalid;

		for( int i = 0; i < screenSprites.Length; i++ )
			screenSprites [i].enabled = false;

		if( audioSource )
			audioSource.Stop();

		wasDancing = false;
	}

	private KeyValuePair<Points, Parts> RandomPointPartPair( List<Points> points, List<Parts> parts )
	{
		return new KeyValuePair<Points, Parts>( points[ UnityEngine.Random.Range( 0, points.Count ) ], parts[ UnityEngine.Random.Range( 0, parts.Count ) ] );
	}

	private IEnumerator DisplayGooglies()
	{
		bool[] spriteEnabled = new bool[ screenSprites.Length ];

		for( int i = 0; i < screenSprites.Length; i++ )
		{
			spriteEnabled[i] = screenSprites[i].enabled;
			screenSprites[i].enabled = false;
		}

		screenSprites[ (int)ScreenType.Googlies ].enabled = true;

		yield return new WaitForSeconds( 2.5f );

		screenSprites[ (int)ScreenType.Googlies ].enabled = false;

		for( int i = 0; i < screenSprites.Length; i++ )
			screenSprites[i].enabled = spriteEnabled[i];
	}

	private IEnumerator DisplayGameOver()
	{
		bool[] spriteEnabled = new bool[ screenSprites.Length ];
		
		for( int i = 0; i < screenSprites.Length; i++ )
		{
			spriteEnabled[i] = screenSprites[i].enabled;
			screenSprites[i].enabled = false;
		}

		screenSprites[ (int)ScreenType.GameOver ].enabled = true;
		
		yield return new WaitForSeconds( 2.5f );
		
		screenSprites[ (int)ScreenType.GameOver ].enabled = false;

		for( int i = 0; i < screenSprites.Length; i++ )
			screenSprites[i].enabled = spriteEnabled[i];
	}
}
