using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PianoOut : MonoBehaviour
{
    private AudioSource[] _audioSource;
    private float _speedOfSoundAttenuation = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        _audioSource = new AudioSource[96]; //128 нот есть в распоряжении нашем, 64-ая - это 3А

        float octaveOffset;
        float semitone_offset;
        float pitch;

        octaveOffset = 2; semitone_offset = -(11 * octaveOffset + octaveOffset); pitch = Mathf.Pow(2f, semitone_offset / 12.0f);
        _audioSource[0] = gameObject.AddComponent<AudioSource>(); _audioSource[0].clip = (AudioClip)Resources.Load("Notes/2C");         _audioSource[0].pitch = pitch;
        _audioSource[1] =gameObject.AddComponent<AudioSource>(); _audioSource[1].clip = (AudioClip)Resources.Load("Notes/2Cs");        _audioSource[1].pitch = pitch;
        _audioSource[2] =gameObject.AddComponent<AudioSource>(); _audioSource[2].clip = (AudioClip)Resources.Load("Notes/2D");        _audioSource[2].pitch = pitch;
        _audioSource[3] =gameObject.AddComponent<AudioSource>(); _audioSource[3].clip = (AudioClip)Resources.Load("Notes/2Ds");        _audioSource[3].pitch = pitch;
        _audioSource[4] =gameObject.AddComponent<AudioSource>(); _audioSource[4].clip = (AudioClip)Resources.Load("Notes/2E");        _audioSource[4].pitch = pitch;
        _audioSource[5] =gameObject.AddComponent<AudioSource>(); _audioSource[5].clip = (AudioClip)Resources.Load("Notes/2F");        _audioSource[5].pitch = pitch;
        _audioSource[6] =gameObject.AddComponent<AudioSource>(); _audioSource[6].clip = (AudioClip)Resources.Load("Notes/2Fs");        _audioSource[6].pitch = pitch;
        _audioSource[7] =gameObject.AddComponent<AudioSource>(); _audioSource[7].clip = (AudioClip)Resources.Load("Notes/2G");        _audioSource[7].pitch = pitch;
        _audioSource[8] =gameObject.AddComponent<AudioSource>(); _audioSource[8].clip = (AudioClip)Resources.Load("Notes/2Gs");        _audioSource[8].pitch = pitch;
        _audioSource[9] =gameObject.AddComponent<AudioSource>(); _audioSource[9].clip = (AudioClip)Resources.Load("Notes/2A");        _audioSource[9].pitch = pitch;
        _audioSource[10] =gameObject.AddComponent<AudioSource>(); _audioSource[10].clip = (AudioClip)Resources.Load("Notes/2As");        _audioSource[10].pitch = pitch;
        _audioSource[11] =gameObject.AddComponent<AudioSource>(); _audioSource[11].clip = (AudioClip)Resources.Load("Notes/2B");        _audioSource[11].pitch = pitch;

        octaveOffset = 1; semitone_offset = -(11 * octaveOffset + octaveOffset); pitch = Mathf.Pow(2f, semitone_offset / 12.0f);
        _audioSource[12] =gameObject.AddComponent<AudioSource>(); _audioSource[12].clip = (AudioClip)Resources.Load("Notes/2C"); _audioSource[12].pitch = pitch;
        _audioSource[13] =gameObject.AddComponent<AudioSource>(); _audioSource[13].clip = (AudioClip)Resources.Load("Notes/2Cs"); _audioSource[13].pitch = pitch;
        _audioSource[14] =gameObject.AddComponent<AudioSource>(); _audioSource[14].clip = (AudioClip)Resources.Load("Notes/2D"); _audioSource[14].pitch = pitch;
        _audioSource[15] =gameObject.AddComponent<AudioSource>(); _audioSource[15].clip = (AudioClip)Resources.Load("Notes/2Ds"); _audioSource[15].pitch = pitch;
        _audioSource[16] =gameObject.AddComponent<AudioSource>(); _audioSource[16].clip = (AudioClip)Resources.Load("Notes/2E"); _audioSource[16].pitch = pitch;
        _audioSource[17] =gameObject.AddComponent<AudioSource>(); _audioSource[17].clip = (AudioClip)Resources.Load("Notes/2F"); _audioSource[17].pitch = pitch;
        _audioSource[18] =gameObject.AddComponent<AudioSource>(); _audioSource[18].clip = (AudioClip)Resources.Load("Notes/2Fs"); _audioSource[18].pitch = pitch;
        _audioSource[19] =gameObject.AddComponent<AudioSource>(); _audioSource[19].clip = (AudioClip)Resources.Load("Notes/2G"); _audioSource[19].pitch = pitch;
        _audioSource[20] =gameObject.AddComponent<AudioSource>(); _audioSource[20].clip = (AudioClip)Resources.Load("Notes/2Gs"); _audioSource[20].pitch = pitch;
        _audioSource[21] =gameObject.AddComponent<AudioSource>(); _audioSource[21].clip = (AudioClip)Resources.Load("Notes/2A"); _audioSource[21].pitch = pitch;
        _audioSource[22] =gameObject.AddComponent<AudioSource>(); _audioSource[22].clip = (AudioClip)Resources.Load("Notes/2As"); _audioSource[22].pitch = pitch;
        _audioSource[23] =gameObject.AddComponent<AudioSource>(); _audioSource[23].clip = (AudioClip)Resources.Load("Notes/2B"); _audioSource[23].pitch = pitch;


        _audioSource[24] =gameObject.AddComponent<AudioSource>(); _audioSource[24].clip = (AudioClip)Resources.Load("Notes/2C");
        _audioSource[25] =gameObject.AddComponent<AudioSource>(); _audioSource[25].clip = (AudioClip)Resources.Load("Notes/2Cs");
        _audioSource[26] =gameObject.AddComponent<AudioSource>(); _audioSource[26].clip = (AudioClip)Resources.Load("Notes/2D");
        _audioSource[27] =gameObject.AddComponent<AudioSource>(); _audioSource[27].clip = (AudioClip)Resources.Load("Notes/2Ds");
        _audioSource[28] =gameObject.AddComponent<AudioSource>(); _audioSource[28].clip = (AudioClip)Resources.Load("Notes/2E");
        _audioSource[29] =gameObject.AddComponent<AudioSource>(); _audioSource[29].clip = (AudioClip)Resources.Load("Notes/2F");
        _audioSource[30] =gameObject.AddComponent<AudioSource>(); _audioSource[30].clip = (AudioClip)Resources.Load("Notes/2Fs");
        _audioSource[31] =gameObject.AddComponent<AudioSource>(); _audioSource[31].clip = (AudioClip)Resources.Load("Notes/2G");
        _audioSource[32] =gameObject.AddComponent<AudioSource>(); _audioSource[32].clip = (AudioClip)Resources.Load("Notes/2Gs");
        _audioSource[33] =gameObject.AddComponent<AudioSource>(); _audioSource[33].clip = (AudioClip)Resources.Load("Notes/2A");
        _audioSource[34] =gameObject.AddComponent<AudioSource>(); _audioSource[34].clip = (AudioClip)Resources.Load("Notes/2As");
        _audioSource[35] =gameObject.AddComponent<AudioSource>(); _audioSource[35].clip = (AudioClip)Resources.Load("Notes/2B");

        _audioSource[36] =gameObject.AddComponent<AudioSource>(); _audioSource[36].clip = (AudioClip)Resources.Load("Notes/3C");
        _audioSource[37] =gameObject.AddComponent<AudioSource>(); _audioSource[37].clip = (AudioClip)Resources.Load("Notes/3Cs");
        _audioSource[38] =gameObject.AddComponent<AudioSource>(); _audioSource[38].clip = (AudioClip)Resources.Load("Notes/3D");
        _audioSource[39] =gameObject.AddComponent<AudioSource>(); _audioSource[39].clip = (AudioClip)Resources.Load("Notes/3Ds");
        _audioSource[40] =gameObject.AddComponent<AudioSource>(); _audioSource[40].clip = (AudioClip)Resources.Load("Notes/3E");
        _audioSource[41] =gameObject.AddComponent<AudioSource>(); _audioSource[41].clip = (AudioClip)Resources.Load("Notes/3F");
        _audioSource[42] =gameObject.AddComponent<AudioSource>(); _audioSource[42].clip = (AudioClip)Resources.Load("Notes/3Fs");
        _audioSource[43] =gameObject.AddComponent<AudioSource>(); _audioSource[43].clip = (AudioClip)Resources.Load("Notes/3G");
        _audioSource[44] =gameObject.AddComponent<AudioSource>(); _audioSource[44].clip = (AudioClip)Resources.Load("Notes/3Gs");
        _audioSource[45] =gameObject.AddComponent<AudioSource>(); _audioSource[45].clip = (AudioClip)Resources.Load("Notes/3A");
        _audioSource[46] =gameObject.AddComponent<AudioSource>(); _audioSource[46].clip = (AudioClip)Resources.Load("Notes/3As");
        _audioSource[47] =gameObject.AddComponent<AudioSource>(); _audioSource[47].clip = (AudioClip)Resources.Load("Notes/3B");

        _audioSource[48] =gameObject.AddComponent<AudioSource>(); _audioSource[48].clip = (AudioClip)Resources.Load("Notes/4C");
        _audioSource[49] =gameObject.AddComponent<AudioSource>(); _audioSource[49].clip = (AudioClip)Resources.Load("Notes/4Cs");
        _audioSource[50] =gameObject.AddComponent<AudioSource>(); _audioSource[50].clip = (AudioClip)Resources.Load("Notes/4D");
        _audioSource[51] =gameObject.AddComponent<AudioSource>(); _audioSource[51].clip = (AudioClip)Resources.Load("Notes/4Ds");
        _audioSource[52] =gameObject.AddComponent<AudioSource>(); _audioSource[52].clip = (AudioClip)Resources.Load("Notes/4E");
        _audioSource[53] =gameObject.AddComponent<AudioSource>(); _audioSource[53].clip = (AudioClip)Resources.Load("Notes/4F");
        _audioSource[54] =gameObject.AddComponent<AudioSource>(); _audioSource[54].clip = (AudioClip)Resources.Load("Notes/4Fs");
        _audioSource[55] =gameObject.AddComponent<AudioSource>(); _audioSource[55].clip = (AudioClip)Resources.Load("Notes/4G");
        _audioSource[56] =gameObject.AddComponent<AudioSource>(); _audioSource[56].clip = (AudioClip)Resources.Load("Notes/4Gs");
        _audioSource[57] =gameObject.AddComponent<AudioSource>(); _audioSource[57].clip = (AudioClip)Resources.Load("Notes/4A");
        _audioSource[58] =gameObject.AddComponent<AudioSource>(); _audioSource[58].clip = (AudioClip)Resources.Load("Notes/4As");
        _audioSource[59] =gameObject.AddComponent<AudioSource>(); _audioSource[59].clip = (AudioClip)Resources.Load("Notes/4B");

        octaveOffset = 1; semitone_offset = 11 * octaveOffset + octaveOffset; pitch = Mathf.Pow(2f, semitone_offset / 12.0f);
        _audioSource[60] =gameObject.AddComponent<AudioSource>(); _audioSource[60].clip = (AudioClip)Resources.Load("Notes/4C"); _audioSource[60].pitch = pitch;
        _audioSource[61] =gameObject.AddComponent<AudioSource>(); _audioSource[61].clip = (AudioClip)Resources.Load("Notes/4Cs"); _audioSource[61].pitch = pitch;
        _audioSource[62] =gameObject.AddComponent<AudioSource>(); _audioSource[62].clip = (AudioClip)Resources.Load("Notes/4D"); _audioSource[62].pitch = pitch;
        _audioSource[63] =gameObject.AddComponent<AudioSource>(); _audioSource[63].clip = (AudioClip)Resources.Load("Notes/4Ds"); _audioSource[63].pitch = pitch;
        _audioSource[64] =gameObject.AddComponent<AudioSource>(); _audioSource[64].clip = (AudioClip)Resources.Load("Notes/4E"); _audioSource[64].pitch = pitch;
        _audioSource[65] =gameObject.AddComponent<AudioSource>(); _audioSource[65].clip = (AudioClip)Resources.Load("Notes/4F"); _audioSource[65].pitch = pitch;
        _audioSource[66] =gameObject.AddComponent<AudioSource>(); _audioSource[66].clip = (AudioClip)Resources.Load("Notes/4Fs"); _audioSource[66].pitch = pitch;
        _audioSource[67] =gameObject.AddComponent<AudioSource>(); _audioSource[67].clip = (AudioClip)Resources.Load("Notes/4G"); _audioSource[67].pitch = pitch;
        _audioSource[68] =gameObject.AddComponent<AudioSource>(); _audioSource[68].clip = (AudioClip)Resources.Load("Notes/4Gs"); _audioSource[68].pitch = pitch;
        _audioSource[69] =gameObject.AddComponent<AudioSource>(); _audioSource[69].clip = (AudioClip)Resources.Load("Notes/4A"); _audioSource[69].pitch = pitch;
        _audioSource[70] =gameObject.AddComponent<AudioSource>(); _audioSource[70].clip = (AudioClip)Resources.Load("Notes/4As"); _audioSource[70].pitch = pitch;
        _audioSource[71] =gameObject.AddComponent<AudioSource>(); _audioSource[71].clip = (AudioClip)Resources.Load("Notes/4B"); _audioSource[71].pitch = pitch;

        octaveOffset = 2; semitone_offset = 11 * octaveOffset + octaveOffset; pitch = Mathf.Pow(2f, semitone_offset / 12.0f);
        _audioSource[72] =gameObject.AddComponent<AudioSource>(); _audioSource[72].clip = (AudioClip)Resources.Load("Notes/4C"); _audioSource[72].pitch = pitch;
        _audioSource[73] =gameObject.AddComponent<AudioSource>(); _audioSource[73].clip = (AudioClip)Resources.Load("Notes/4Cs"); _audioSource[73].pitch = pitch;
        _audioSource[74] =gameObject.AddComponent<AudioSource>(); _audioSource[74].clip = (AudioClip)Resources.Load("Notes/4D"); _audioSource[74].pitch = pitch;
        _audioSource[75] =gameObject.AddComponent<AudioSource>(); _audioSource[75].clip = (AudioClip)Resources.Load("Notes/4Ds"); _audioSource[75].pitch = pitch;
        _audioSource[76] =gameObject.AddComponent<AudioSource>(); _audioSource[76].clip = (AudioClip)Resources.Load("Notes/4E"); _audioSource[76].pitch = pitch;
        _audioSource[77] =gameObject.AddComponent<AudioSource>(); _audioSource[77].clip = (AudioClip)Resources.Load("Notes/4F"); _audioSource[77].pitch = pitch;
        _audioSource[78] =gameObject.AddComponent<AudioSource>(); _audioSource[78].clip = (AudioClip)Resources.Load("Notes/4Fs"); _audioSource[78].pitch = pitch;
        _audioSource[79] =gameObject.AddComponent<AudioSource>(); _audioSource[79].clip = (AudioClip)Resources.Load("Notes/4G"); _audioSource[79].pitch = pitch;
        _audioSource[80] =gameObject.AddComponent<AudioSource>(); _audioSource[80].clip = (AudioClip)Resources.Load("Notes/4Gs"); _audioSource[80].pitch = pitch;
        _audioSource[81] =gameObject.AddComponent<AudioSource>(); _audioSource[81].clip = (AudioClip)Resources.Load("Notes/4A"); _audioSource[81].pitch = pitch;
        _audioSource[82] =gameObject.AddComponent<AudioSource>(); _audioSource[82].clip = (AudioClip)Resources.Load("Notes/4As"); _audioSource[82].pitch = pitch;
        _audioSource[83] =gameObject.AddComponent<AudioSource>(); _audioSource[83].clip = (AudioClip)Resources.Load("Notes/4B"); _audioSource[83].pitch = pitch;

        octaveOffset = 3; semitone_offset = 11 * octaveOffset + octaveOffset; pitch = Mathf.Pow(2f, semitone_offset / 12.0f);
        _audioSource[84] =gameObject.AddComponent<AudioSource>(); _audioSource[84].clip = (AudioClip)Resources.Load("Notes/4C"); _audioSource[84].pitch = pitch;
        _audioSource[85] =gameObject.AddComponent<AudioSource>(); _audioSource[85].clip = (AudioClip)Resources.Load("Notes/4Cs"); _audioSource[85].pitch = pitch;
        _audioSource[86] =gameObject.AddComponent<AudioSource>(); _audioSource[86].clip = (AudioClip)Resources.Load("Notes/4D"); _audioSource[86].pitch = pitch;
        _audioSource[87] =gameObject.AddComponent<AudioSource>(); _audioSource[87].clip = (AudioClip)Resources.Load("Notes/4Ds"); _audioSource[87].pitch = pitch;
        _audioSource[88] =gameObject.AddComponent<AudioSource>(); _audioSource[88].clip = (AudioClip)Resources.Load("Notes/4E"); _audioSource[88].pitch = pitch;
        _audioSource[89] =gameObject.AddComponent<AudioSource>(); _audioSource[89].clip = (AudioClip)Resources.Load("Notes/4F"); _audioSource[89].pitch = pitch;
        _audioSource[90] =gameObject.AddComponent<AudioSource>(); _audioSource[90].clip = (AudioClip)Resources.Load("Notes/4Fs"); _audioSource[90].pitch = pitch;
        _audioSource[91] =gameObject.AddComponent<AudioSource>(); _audioSource[91].clip = (AudioClip)Resources.Load("Notes/4G"); _audioSource[91].pitch = pitch;
        _audioSource[92] =gameObject.AddComponent<AudioSource>(); _audioSource[92].clip = (AudioClip)Resources.Load("Notes/4Gs"); _audioSource[92].pitch = pitch;
        _audioSource[93] =gameObject.AddComponent<AudioSource>(); _audioSource[93].clip = (AudioClip)Resources.Load("Notes/4A"); _audioSource[93].pitch = pitch;
        _audioSource[94] =gameObject.AddComponent<AudioSource>(); _audioSource[94].clip = (AudioClip)Resources.Load("Notes/4As"); _audioSource[94].pitch = pitch;
        _audioSource[95] =gameObject.AddComponent<AudioSource>(); _audioSource[95].clip = (AudioClip)Resources.Load("Notes/4B"); _audioSource[95].pitch = pitch;
    }

    // Update is called once per frame
    void Update()
    {
        AttenuateIfNeeded();
    }

    public void PlayNote(byte note, float volume=1)
    {
        _audioSource[note].Stop();
        _audioSource[note].volume = volume;
        _audioSource[note].Play();
    }

    private void AttenuateIfNeeded()
    {
        foreach(AudioSource aus in _audioSource)
        if ( aus.volume >= 0.1)
        {
                aus.volume = aus.volume - _speedOfSoundAttenuation * Time.deltaTime;
        }
    }
}
