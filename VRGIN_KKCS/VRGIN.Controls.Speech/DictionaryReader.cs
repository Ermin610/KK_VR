using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VRGIN.Core;

namespace VRGIN.Controls.Speech;

public class DictionaryReader
{
	private Dictionary<string, VoiceCommand> _Dictionary = new Dictionary<string, VoiceCommand>();

	public Type BaseType { get; private set; }

	public DictionaryReader(Type type)
	{
		if (IsVoiceCommandType(type))
		{
			BaseType = type;
		}
		else
		{
			BaseType = typeof(VoiceCommand);
			VRLog.Error("Invalid VoiceCommand type! {0}", type);
		}
		BuildCommandDictionary();
	}

	public void LoadDictionary(string path)
	{
		if (!File.Exists(path))
		{
			return;
		}
		using StreamReader streamReader = new StreamReader(File.OpenRead(path), Encoding.UTF8);
		VoiceCommand value = null;
		while (!streamReader.EndOfStream)
		{
			string text = streamReader.ReadLine().Trim().ToLowerInvariant();
			if (IsCommand(text))
			{
				if (_Dictionary.TryGetValue(ExtractCommand(text), out value))
				{
					value.Texts.Clear();
				}
			}
			else if (value != null && text.Length > 0)
			{
				value.Texts.Add(text);
			}
		}
	}

	public void SaveDictionary(string path)
	{
		EnsurePath(path);
		using StreamWriter streamWriter = new StreamWriter(File.Open(path, FileMode.OpenOrCreate), Encoding.UTF8);
		streamWriter.BaseStream.SetLength(0L);
		foreach (FieldInfo item in ExtractCommands(BaseType))
		{
			streamWriter.WriteLine("[{0}]", item.Name);
			if (item.GetValue(null) is VoiceCommand voiceCommand)
			{
				foreach (string text in voiceCommand.Texts)
				{
					streamWriter.WriteLine(text);
				}
			}
			streamWriter.WriteLine();
		}
	}

	private void EnsurePath(string path)
	{
		if (!File.Exists(path))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
		}
	}

	private void BuildCommandDictionary()
	{
		foreach (FieldInfo item in ExtractCommands(BaseType))
		{
			if (item.GetValue(null) is VoiceCommand value)
			{
				_Dictionary[item.Name.ToLowerInvariant()] = value;
			}
		}
	}

	public static IEnumerable<FieldInfo> ExtractCommands(Type type)
	{
		return from field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
			where IsVoiceCommandType(field.FieldType)
			select field;
	}

	public static IEnumerable<VoiceCommand> ExtractCommandObjects(Type type)
	{
		return from t in ExtractCommands(type)
			select t.GetValue(null) as VoiceCommand into t
			where t != null
			select t;
	}

	private static bool IsVoiceCommandType(Type type)
	{
		return typeof(VoiceCommand).IsAssignableFrom(type);
	}

	private static bool IsCommand(string line)
	{
		if (line.Length > 2 && line.StartsWith("[", StringComparison.Ordinal))
		{
			return line.EndsWith("]", StringComparison.Ordinal);
		}
		return false;
	}

	private static string ExtractCommand(string line)
	{
		return line.Substring(1, line.Length - 2);
	}
}
