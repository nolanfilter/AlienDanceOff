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
		//TODO procedurally set points/parts map based on connections
		pointToPartMap.Add( Points.Head, Parts.Eyestalk );
		pointToPartMap.Add( Points.Butt, Parts.Tail );
		pointToPartMap.Add( Points.Shoulder, Parts.SleepyFriend );
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

		foreach( KeyValuePair<Points, Parts> kvp in pointToPartMap )
			Debug.Log( "" + kvp.Value + " = " + partValues[ (int)pointToPartMap[ kvp.Key ] ] );
	}

	private void EvaluateInputValue( int inputValue )
	{
		Points point = (Points)( inputValue / numPoints );
		int relativeValue = inputValue%2;

		if( pointToPartMap.ContainsKey( point ) )
			partValues[ (int)pointToPartMap[ point ] ] += ( relativeValue == 0 ? -1 : 1 );
	}
}
