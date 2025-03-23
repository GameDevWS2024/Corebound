import argparse
import azure.cognitiveservices.speech as speechsdk # type: ignore
import sys

def synthesize_speech(text_to_speak, vname, vstyle, intensity):
    # Azure Speech Service credentials
    subscription_key = "7bSNiJW3KBaDULUXieFtkeTBtdufencnISX4CVSudrbBf107AEurJQQJ99AKAC5RqLJXJ3w3AAAYACOGICZW"
    region = "westeurope"

    ##############################
    audioOutput = "D:/Corebound/scripts/output.wav"
    ##############################

    # Create a Speech Config object
    speech_config = speechsdk.SpeechConfig(subscription=subscription_key, region=region)

    # Set the voice name
    speech_config.speech_synthesis_voice_name = vname  

    # Create SSML text
    ssml_text = f"""
    <speak xmlns:mstts="http://www.w3.org/2001/mstts" version='1.0' xml:lang='en-US'>
        <voice name='{vname}'>
            <mstts:express-as style='{vstyle}' styledegree="2">
                <prosody rate='+0%' pitch='medium' volume='+0%'>
                    <s emotion='{vstyle}' level='{intensity}'>{text_to_speak}</s>
                </prosody>
            </mstts:express-as>
        </voice>
    </speak>
    """
    
    ##################################
        # Create an audio output configuration
    audio_config = speechsdk.AudioConfig(filename=audioOutput)

      # Create a Speech Synthesizer with audio output configuration
    speech_synthesizer = speechsdk.SpeechSynthesizer(speech_config=speech_config, audio_config=audio_config)
    ########################

    # Synthesize speech with SSML
    result = speech_synthesizer.speak_ssml_async(ssml_text).get()

    # Check the result
    if result.reason == speechsdk.ResultReason.SynthesizingAudioCompleted:
        print("Speech synthesis completed. Audio saved to: {audioOutput}")
    elif result.reason == speechsdk.ResultReason.Canceled:
        cancellation_details = result.cancellation_details
        print(f"Speech synthesis canceled: {cancellation_details.reason}")
        if cancellation_details.reason == speechsdk.CancellationReason.Error:
            print(f"Error details: {cancellation_details.error_details}")
    sys.exit(0)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Synthesize speech using Azure Cognitive Services.')
    parser.add_argument('text_to_speak', type=str, help='The text to synthesize into speech.')
    parser.add_argument('vname', type=str, help='The voice name for speech synthesis.')
    parser.add_argument('vstyle', type=str, help='The style of the voice (e.g., hopeful).')
    parser.add_argument('intensity', type=str, help='The intensity of the emotion (low, medium, high).')

    args = parser.parse_args()

    # Call the synthesis function with command-line arguments
    synthesize_speech(args.text_to_speak, args.vname, args.vstyle, args.intensity)
