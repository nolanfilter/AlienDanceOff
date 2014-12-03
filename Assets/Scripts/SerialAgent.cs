using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;


public class SerialAgent : MonoBehaviour {

	public string portName = "/dev/tty.usbmodem1411";

	private SerialPort serial;

	private static SerialAgent mInstance = null;
	public static SerialAgent instance
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
			Debug.LogError( string.Format( "Only one instance of SerialAgent allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			Destroy( gameObject );
			return;
		}
		
		mInstance = this;
	}

	void Start()
	{
		try 
		{
			serial = new SerialPort();
			
			serial.ReadBufferSize = 8192;
			serial.WriteBufferSize = 128;
			
			serial.PortName = portName;
			serial.BaudRate = 9600;
			serial.Parity = Parity.None;
			serial.StopBits = StopBits.One;
			
			serial.Open();
		}
		catch (Exception ex) 
		{

		}
	}

	public static string SerialReadLine()
	{
		if( instance )
			return instance.internalSerialReadLine();

		return "";
	}

	private string internalSerialReadLine()
	{
		string serialLine = "";

		try 
		{
			serialLine = serial.ReadLine();
		}
		catch (Exception ex)
		{
			
		}

		return serialLine;
	}
}
