using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameAgent : MonoBehaviour {

	public enum Points
	{
		Head = 0,
		Butt = 1,
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

	private Dictionary<Points, Parts> pointToPartMap = new Dictionary<Points, Parts>();
	private int[] partValues = new int[ Enum.GetNames( typeof( Parts ) ).Length - 1 ];
	private int numPoints = Enum.GetNames( typeof( Points ) ).Length - 1;

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
	}

	void Start()
	{
		SetPointPartPairs();

		// "Default" connections
		//pointToPartMap.Add( Points.Head, Parts.Eyestalk );
		//pointToPartMap.Add( Points.Butt, Parts.Tail );
		//pointToPartMap.Add( Points.Shoulder, Parts.SleepyFriend );
	}

	void Update()
	{
		string currentInputString = SerialAgent.SerialReadLine();
		int inputValue;

		for( int i = 0; i < currentInputString.Length; i++ )
			if( int.TryParse( "" + currentInputString[i], out inputValue ) )
				EvaluateInputValue( inputValue );

		for( int i = 0; i < partValues.Length; i++ )
			partValues[i] = Mathf.RoundToInt( Mathf.Clamp01( partValues[i] ) );

		//foreach( KeyValuePair<Points, Parts> kvp in pointToPartMap )
		//	Debug.Log( "" + kvp.Value + " = " + partValues[ (int)pointToPartMap[ kvp.Key ] ] );

		if( Input.GetKeyDown( KeyCode.Space ) )
			SetPointPartPairs();

		if( Input.GetKeyDown( KeyCode.LeftArrow ) )
		{
			int currentDifficultyInt = (int)currentDifficulty;
			
			if( currentDifficultyInt > 0 )
			{
				currentDifficulty = (Difficulty)(currentDifficultyInt - 1);
				SetPointPartPairs();
			}
		}

		if( Input.GetKeyDown( KeyCode.RightArrow ) )
		{
			int currentDifficultyInt = (int)currentDifficulty;

			if( currentDifficultyInt < 3 )
			{
				currentDifficulty = (Difficulty)(currentDifficultyInt + 1);
				SetPointPartPairs();
			}
		}
	}

	private void EvaluateInputValue( int inputValue )
	{
		Points point = (Points)( inputValue / numPoints );
		int relativeValue = inputValue%2;

		if( pointToPartMap.ContainsKey( point ) )
			partValues[ (int)pointToPartMap[ point ] ] += ( relativeValue == 0 ? -1 : 1 );
	}

	private void SetPointPartPairs()
	{
		pointToPartMap.Clear();

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

			pointToPartMap.Add( pointPartPair.Key, pointPartPair.Value );

			possiblePoints.Remove( pointPartPair.Key );
			possibleParts.Remove( pointPartPair.Value );
		}

		Debug.Log( currentDifficulty );

		foreach( KeyValuePair<Points, Parts> kvp in pointToPartMap )
			Debug.Log( "" + kvp.Key + " gets " + kvp.Value );
	}

	private KeyValuePair<Points, Parts> RandomPointPartPair( List<Points> points, List<Parts> parts )
	{
		return new KeyValuePair<Points, Parts>( points[ UnityEngine.Random.Range( 0, points.Count ) ], parts[ UnityEngine.Random.Range( 0, parts.Count ) ] );
	}
}
