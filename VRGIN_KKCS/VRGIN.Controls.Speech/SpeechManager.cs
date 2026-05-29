using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SpeechTransport;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using VRGIN.Core;

namespace VRGIN.Controls.Speech;

public class SpeechManager : ProtectedBehaviour
{
	private Thread receiveThread;

	private UdpClient client;

	private SpeechResult? result;

	private const string LOCALHOST = "127.0.0.1";

	private const string CAMEL_CASE_REGEX = "(\\B[A-Z]+?(?=[A-Z][^A-Z])|\\B[A-Z]+?(?=[^A-Z]))";

	private const string DICT_PATH = "UserData\\dictionaries";

	private string _ServerPath;

	private object LOCK = new object();

	private static Process server;

	public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized = delegate
	{
	};

	protected override void OnStart()
	{
		base.OnStart();
		InitializeDictionary();
		StartServer();
	}

	private void StartServer()
	{
		_ServerPath = Application.dataPath + "/../Plugins/VR/SpeechServer.exe";
		if (!File.Exists(_ServerPath))
		{
			VRLog.Error("Could not find SpeechServer at {0}", _ServerPath);
			((Behaviour)this).enabled = false;
			return;
		}
		FileInfo fileInfo = new FileInfo(_ServerPath);
		if (server == null)
		{
			VRLog.Info(fileInfo.FullName);
			server = new Process();
			server.StartInfo.FileName = fileInfo.FullName;
			server.StartInfo.UseShellExecute = false;
			server.StartInfo.CreateNoWindow = true;
			server.StartInfo.Arguments = $"--words \"{GetVoiceCommands()}\" --locale {VR.Settings.Locale}";
			server.StartInfo.RedirectStandardOutput = true;
			server.StartInfo.RedirectStandardError = true;
			server.StartInfo.RedirectStandardInput = true;
			server.StartInfo.StandardOutputEncoding = Encoding.UTF8;
			server.StartInfo.StandardErrorEncoding = Encoding.UTF8;
			server.OutputDataReceived += OnOutputReceived;
			server.ErrorDataReceived += OnErrorReceived;
			VRLog.Info("Starting speech server: {0}", server.StartInfo.Arguments);
			server.Start();
			server.BeginOutputReadLine();
			server.BeginErrorReadLine();
			VRLog.Info("Started!");
		}
	}

	private void OnErrorReceived(object sender, DataReceivedEventArgs e)
	{
		VRLog.Error(e.Data);
	}

	private void InitializeDictionary()
	{
		string text = CombinePath(Application.dataPath, "..", "UserData\\dictionaries", VR.Settings.Locale + ".txt");
		DictionaryReader dictionaryReader = new DictionaryReader(VR.Context.VoiceCommandType);
		VRLog.Info("Loading dictionary at {0}...", text);
		dictionaryReader.LoadDictionary(text);
		VRLog.Info("Saving dictionary at {0}...", text);
		dictionaryReader.SaveDictionary(text);
	}

	private string CombinePath(params string[] paths)
	{
		string path = paths[0];
		for (int i = 1; i < paths.Length; i++)
		{
			path = Path.Combine(path, paths[i]);
		}
		return path;
	}

	private void OnOutputReceived(object sender, DataReceivedEventArgs e)
	{
		try
		{
			lock (LOCK)
			{
				result = SpeechResult.Deserialize(e.Data);
				VRLog.Info("RECEIVED MESSAGE: " + e.Data);
			}
		}
		catch (Exception obj)
		{
			VRLog.Error(obj);
		}
	}

	private string GetVoiceCommands()
	{
		return string.Join(";", DictionaryReader.ExtractCommandObjects(VR.Context.VoiceCommandType).SelectMany((VoiceCommand command) => command.Texts).ToArray());
	}

	private void OnDisable()
	{
		if (receiveThread != null)
		{
			receiveThread.Abort();
			receiveThread = null;
		}
		client.Close();
	}

	protected override void OnUpdate()
	{
		lock (LOCK)
		{
			if (result.HasValue)
			{
				((Component)this).SendMessage("OnSpeech", (object)result.Value);
				this.SpeechRecognized(this, new SpeechRecognizedEventArgs(result.Value));
				result = null;
			}
		}
	}
}
