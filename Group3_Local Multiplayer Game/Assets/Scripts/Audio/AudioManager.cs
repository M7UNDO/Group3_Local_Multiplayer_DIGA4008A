using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Controller
{
    public class AudioManager : MonoBehaviour
    {
        public AudioMixer masterMixer;
        public Slider masterVolumeSlider;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public Slider uiVolumeSlider;

        private void Start()
        {
            //starting volume for the sliders
            float masterVol,
                musicVol,
                sfxVol,
                uiVol;

            // Current volume levels from the mixer
            masterMixer.GetFloat("MasterVolume", out masterVol);
            masterMixer.GetFloat("MusicVolume", out musicVol);
            masterMixer.GetFloat("SFXVolume", out sfxVol);
            masterMixer.GetFloat("UIVolume", out uiVol);

            masterVolumeSlider.value = masterVol;
            musicVolumeSlider.value = musicVol;
            sfxVolumeSlider.value = sfxVol;
            uiVolumeSlider.value = uiVol;

            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
            uiVolumeSlider.onValueChanged.AddListener(SetUIVolume);
        }

        public void SetMasterVolume(float sliderValue)
        {
            masterMixer.SetFloat("MasterVolume", sliderValue);
            print("Master Volume set to: " + sliderValue);
        }

        public void SetMusicVolume(float sliderValue)
        {
            masterMixer.SetFloat("MusicVolume", sliderValue);
            print("Music Volume set to: " + sliderValue);
        }

        public void SetSfxVolume(float sliderValue)
        {
            masterMixer.SetFloat("SFXVolume", sliderValue);
            print("SFX Volume set to: " + sliderValue);
        }

        public void SetUIVolume(float sliderValue)
        {
            masterMixer.SetFloat("UIVolume", sliderValue);
            print("UI Volume set to: " + sliderValue);
        }
    }
}