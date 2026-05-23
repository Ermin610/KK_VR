using System;
using System.Diagnostics;
using System.IO;

namespace VRGIN.Core;

public class VRLog
{
	public enum LogMode
	{
		Debug,
		Info,
		Warning,
		Error
	}

	private static string LOG_PATH;

	private static object _LOCK;

	private static StreamWriter S_Handle;

	public static LogMode Level;

	static VRLog()
	{
		LOG_PATH = "vr.log";
		_LOCK = new object();
		Level = LogMode.Info;
		S_Handle = new StreamWriter(File.OpenWrite(LOG_PATH));
		S_Handle.BaseStream.SetLength(0L);
		S_Handle.AutoFlush = true;
	}

	protected VRLog()
	{
	}

	public static void Debug(string text, params object[] args)
	{
		Log(text, args, LogMode.Debug);
	}

	public static void Info(string text, params object[] args)
	{
		Log(text, args, LogMode.Info);
	}

	public static void Warn(string text, params object[] args)
	{
		Log(text, args, LogMode.Warning);
	}

	public static void Error(string text, params object[] args)
	{
		Log(text, args, LogMode.Error);
	}

	public static void Debug(object obj)
	{
		Log("{0}", new object[1] { obj }, LogMode.Debug);
	}

	public static void Info(object obj)
	{
		Log("{0}", new object[1] { obj }, LogMode.Info);
	}

	public static void Warn(object obj)
	{
		Log("{0}", new object[1] { obj }, LogMode.Warning);
	}

	public static void Error(object obj)
	{
		Log("{0}", new object[1] { obj }, LogMode.Error);
	}

	public static void Log(string text, object[] args, LogMode severity)
	{
		try
		{
			if (severity < Level)
			{
				return;
			}
			string value = string.Format(Format(text, severity), args);
			lock (_LOCK)
			{
				Console.WriteLine(value);
				S_Handle.WriteLine(value);
			}
		}
		catch (Exception value2)
		{
			Console.WriteLine(value2);
		}
	}

	private static string Format(string text, LogMode mode)
	{
		StackFrame frame = new StackTrace(3).GetFrame(0);
		return string.Format("[{0}][{1}][{3}#{4}] {2}", DateTime.Now.ToString("HH':'mm':'ss"), mode.ToString().ToUpper(), text, frame.GetMethod().DeclaringType.Name, frame.GetMethod().Name, frame.GetFileLineNumber());
	}
}
