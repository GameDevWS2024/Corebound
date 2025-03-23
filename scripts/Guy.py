import os
import azure.cognitiveservices.speech as speechsdk
import argparse
import requests
import json

def get_voice_styles(region, voice_name):
    """Retrieves available styles for a given voice from the Azure Speech service."""
    
    url = f"https://{region}.tts.speech.microsoft.com/cognitiveservices/voices/list"
    headers = {
        "Ocp-Apim-Subscription-Key": '7bSNiJW3KBaDULUXieFtkeTBtdufencnISX4CVSudrbBf107AEurJQQJ99AKAC5RqLJXJ3w3AAAYACOGICZW'
    }
    
    try:
        response = requests.get(url, headers=headers)
        response.raise_for_status()  # Raise HTTPError for bad responses (4xx or 5xx)
        voices = response.json()
        
        for voice in voices:
            if voice["ShortName"] == voice_name:
                if "StyleList" in voice:
                    return voice["StyleList"]
                else:
                    return ["general"] # If no style list, general is the only available style.
        return ["general"] #If the voice is not found, return general.
    except requests.exceptions.RequestException as e:
        print(f"Error retrieving voice styles: {e}")
        return ["general"]

def synthesize_speech(text, style, voice_name='en-US-GuyNeural'):
    """Synthesizes speech from the given text with optional voice and style."""

    speech_config = speechsdk.SpeechConfig(subscription='7bSNiJW3KBaDULUXieFtkeTBtdufencnISX4CVSudrbBf107AEurJQQJ99AKAC5RqLJXJ3w3AAAYACOGICZW', region='westeurope')
    audio_config = speechsdk.audio.AudioOutputConfig(filename="output.wav") # use_default_speaker=True     for playback

    speech_config.speech_synthesis_voice_name = voice_name

    speech_synthesizer = speechsdk.SpeechSynthesizer(speech_config=speech_config, audio_config=audio_config)

    # Apply speech style if specified
    if style != 'general':
        ssml_text = f"""
        <speak xmlns:mstts="http://www.w3.org/2001/mstts" version='1.0' xml:lang='en-US'>
            <voice name='{voice_name}'>
                <mstts:express-as style='{style}' styledegree="2">
                    <prosody rate='+10%' pitch='medium' volume='+20%'>
                        <s emotion='{style}' level='high'>{text}</s>
                    </prosody>
                </mstts:express-as>
            </voice>
        </speak>
        """

        speech_synthesis_result = speech_synthesizer.speak_ssml_async(ssml_text).get()
    else:
        speech_synthesis_result = speech_synthesizer.speak_text_async(text).get()

    if speech_synthesis_result.reason == speechsdk.ResultReason.SynthesizingAudioCompleted:
        print(f"Speech synthesized for text [{text}] with style [{style}]")
    elif speech_synthesis_result.reason == speechsdk.ResultReason.Canceled:
        cancellation_details = speech_synthesis_result.cancellation_details
        print(f"Speech synthesis canceled: {cancellation_details.reason}")
        if cancellation_details.reason == speechsdk.CancellationReason.Error:
            if cancellation_details.error_details:
                print(f"Error details: {cancellation_details.error_details}")
                print("Did you set the speech resource key and region values?")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Synthesize speech from text.")
    parser.add_argument("text", help="The text to synthesize.")
    parser.add_argument("--voice", default="en-US-GuyNeural", help="The voice to use (e.g., en-US-AvaMultilingualNeural).")
    parser.add_argument("--style", default="general", choices=['newscast', 'angry', 'cheerful', 'sad', 'excited', 'friendly', 'terrified', 'shouting', 'unfriendly', 'whispering', 'hopeful'], help="The speech style to use.")

    args = parser.parse_args()

    available_styles = get_voice_styles('westeurope', args.voice)
    print(f"Available styles for {args.voice}: {available_styles}")

    synthesize_speech(args.text, args.style, args.voice)