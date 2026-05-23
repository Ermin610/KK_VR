using System;
using SpeechTransport;

namespace VRGIN.Controls.Speech;

public class SpeechRecognizedEventArgs : EventArgs
{
	public SpeechResult Result { get; private set; }

	public SpeechRecognizedEventArgs(SpeechResult result)
	{
		Result = result;
	}
}
