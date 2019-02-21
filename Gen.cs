using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellularAutamata;
using UnityEngine.UI;
using B83.MathHelpers;

public class Gen : MonoBehaviour
{
    // Start is called before the first frame update
    GameObject[,,] objects;
    public short lenght = 40;
    public short height = 40;
    public short width = 10;
    public string rule = "26 [0]/13/36";
    public GameObject cube;

    //public CellularAutamata3D ca;
    public CellularAutamata3D ca;

    public InputField inputLenght;
    public InputField inputHeight;
    public InputField inputWidth;
    public InputField inputRule;

    public InputField inputI;
    public InputField inputJ;
    public InputField inputK;
    public InputField inputDrawVal;
    public Text textDebug;

    public Toggle inputToggle;
    public Dropdown dropdown;

    public InputField inputFrameRate;
    public Text textInfo;

    private int generation = 0;
    private NeuroNet n;

    private PianoOut po;

    public InputField inputTextToOkagamga;
    public Toggle inputToggleSpeak, inputToggleSpeakRepeat;

    void Start()
    {
       
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>( Microphone.devices));

        n = DNA.Wake();
        ca = n.ca;

        lenght = ca.lenght; height = ca.height; width = ca.width;
        rule = ca.rule.rule; inputRule.text = rule;
        inputLenght.text = lenght.ToString(); inputHeight.text = height.ToString(); inputWidth.text = width.ToString();

        objects = new GameObject[lenght, height, width];
        for (int i = 0; i < lenght; i++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int k = 0; k < width; k++)
                {
                    objects[i, j, k] = Instantiate(cube,
                                 new Vector3(i * 1.1f, j * 1.1f, k * 1.1f),
                                 Quaternion.identity);
                    objects[i, j, k].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
                }
            }
        }

    }


    // Update is called once per frame
    private bool run = false;
    bool run_audio = false;
    AudioSource audioSource;

    int lenght_mic_buffer;

    void Update()
    {

        string sinfo;


        if (run_audio)
        {
            DrawAudio();
            ca.queChangeAgeFunc();
            sinfo ="Generations: " + generation.ToString()+"\r\nMic: ON | Buffer: "+ lenght_mic_buffer+"\tHz:"+Hz.ToString();
        }
        else sinfo = "Generations: " + generation.ToString() + "\r\nMic: off";

        textInfo.text = sinfo;
        if (run)
        {
            ca.queChangeAgeFunc();
            n.Do();
            //играем музыку
            PlayMusic();

            n.DoServices();

            int hyyy = (n.service.hyppocamp_value * 1 / Service.const_hyppo_ave + n.service.hyppocamp_prev * (Service.const_hyppo_ave - 1) / Service.const_hyppo_ave);
            if (hyyy > 0) textDebug.text = hyyy.ToString();
        }

        repeatTextFunc();
    }

    uint ceur_bit = 0;
    uint breathe_bit = 0;

    private void FixedUpdate()
    {
        if (run)
        {

            //сердцебиение и дыхание
            for (int j = 0; j < height; j++)
            {
                ca.ChangeAgeFast(53, (short)j, 10, 0);
                ca.ChangeAgeFast(53, (short)j, 12, 0);
            }
            for (int j=0;j< breathe_bit%(height-1);j++)
            {
                ca.ChangeAge(53, (short)j, 10, 1);
            }
            for (int j = 0; j < ceur_bit%(height >> 1); j++)
            {
                ca.ChangeAge(53, (short)(height-j-1), 12, 1);
            }
            breathe_bit++; ceur_bit++;         
            
            Step();
        }
    }
    public void textToBox(string text)
    {
        byte[] b = System.Text.Encoding.ASCII.GetBytes(text);
        //рисуем в КА на стороне i-k в 
        for (int i= 0; i<b.Length && i<lenght; i++)
        {
            for (int j=0;j<8;j++)//8бит
            {
                ca.ChangeAge((short)(i+lenght/2+1), (short)j, 0 , (short)((b[i] >> j) & 1));
                ca.ChangeAge((short)(i + lenght / 2 + 1), (short)j, (short)(width-1), (short)((b[i] >> j) & 1));
               
            }
        }
    }

    int repeatTextToOkagamga = 0;
    public void repeatTextFunc()
    {

        if (inputToggleSpeakRepeat.isOn && ++repeatTextToOkagamga>150)
        {
            inputTextOkagamga();
        }

    }
    public void inputTextOkagamga()
    {
        repeatTextToOkagamga = 0;
        if (inputTextToOkagamga.text.Length > 0)
        {
            textToBox(inputTextToOkagamga.text);
            if(inputToggleSpeak.isOn)
            {
                WindowsVoice.speak(inputTextToOkagamga.text);
            }
        }
        
    }

    private void PlayMusic()
    {
        
        float volume=0.0f;

        for (int i =30; i<= 40; i++)
        {
            for (int j=height/2-10;j<height/2+10;j++)
                volume = volume + ca.cell[i, j, width / 2] * 3+ ca.cell[i, j, width / 2-1]+ ca.cell[i, j, width / 2+1];   
        }
        if (volume < 1) volume = 0;        
        else volume = (Mathf.Log10(Mathf.Log(volume)) ) * 5f;

        //if (volume > 1) volume = 1; else if (volume < 0) volume = 0;
        string blabla="";
        if (volume>0.5)
        {//говорит и показыват КА

            byte[] b = new byte[12];
            for (int i=29;i<=40;i++)
            {
                int v = 0;

                for (int j = 1; j < height - 1; j++)
                {
                    int tt = 0;
                    for (int k = 1; k < width - 1; k++)
                    {
                        tt += ca.cell[i, j, k];
                        v += tt;

                    }

                    /*
                    //рисуем то, что говорим на стенке j-k
                    tt = tt % n.ca.rule.max_age;
                    ca.ChangeAge(0, (short)i, (short)j, (short)tt);//это не ошибка! i=0, i=j, j=k
                    */
                    
                }
                v = v % (122 - 97) + 97;
                b[i-29]=  (byte)v;
                
            }
            blabla = System.Text.Encoding.ASCII.GetString(b);
            textToBox(blabla); //рисуем то, что говорим
            Debug.Log(blabla);
            
            WindowsVoice.speak(blabla);
            n.dna.WriteDebug(blabla);
            
        }
        textDebug.text = "v: "+ volume.ToString() + " bla: " + blabla;
        /*
        float p1 = 0.0f;
        if (volume > 0.05)
        {
            for (int i=1;i<lenght-1 ;i++)
            {
                for (int j = height / 2 - 3; j < height / 2 + 4; j++)
                    for(int k = width / 2 - 3; k < width / 2 + 4; k++)
                        p1 += ca.cell[i, j, k];
            }
            p1 = Mathf.Log(p1) * 5;
            if (p1 > 95) p1 = 95; else if (p1 < 0) p1 = 0;
            po.PlayNote((byte)p1, volume);
        }
        */
    }

    private void Step()
    {
        generation++;
        ca.NextStep();
        if (inputToggle.isOn)
        for (short i = 0; i < lenght; i++)
        {
            for (short j = 0; j < height; j++)
            {
                for (short k = 0; k < width; k++)
                {
                    //Debug.Log(i.ToString()+" "+j.ToString()+" "+k.ToString());
                    //Debug.Log((int) (ca.cell[i, j, k].));
                    if (ca.cell[i, j, k] == 0)
                    {
                        objects[i, j, k].SetActive(false);
                        //objects[i, j, k].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 1.0f, 1.0f, 0f);
                    }
                    else
                    {
                        float al = 1f / ca.rule.max_age * ((ca.rule.max_age + 1) - ca.cell[i, j, k]);
                        objects[i, j, k].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 1f - al, 1f - al, al);
                        objects[i, j, k].SetActive(true);
                    }
                }
            }
        }
    }

    public void ButtonRunStop_Click()
    {
        if (po is null) po = GetComponent<PianoOut>();

        int t = int.Parse(inputFrameRate.text);
        if (t < 15) { t = 15; inputFrameRate.text = "15"; }
        else if (t > 1000) { t = 1000; inputFrameRate.text = "1000"; }
        Time.fixedDeltaTime = t / 100.0f;
        run = !run;

        if (run)
        {
            po.PlayNote(60); po.PlayNote(64); po.PlayNote(70);
            n.dna.WriteDebug("start");
        }
        else
        {
            po.PlayNote(55); po.PlayNote(75); po.PlayNote(90);
        }
    }

    public void ButtonStep_Click()
    {
        if (run)run = false;
        else Step();
    }

    public void ButtonGen_Click()
    {
        run = false; generation = 0;
        if(run_audio) ButtonGetStopMic_Click();

        foreach(GameObject o in objects) Destroy(o);

        lenght = short.Parse(inputLenght.text);
        height = short.Parse(inputHeight.text);
        width = short.Parse(inputWidth.text);
        rule = inputRule.text;

        objects = new GameObject[lenght, height, width];
        for (int i = 0; i < lenght; i++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int k = 0; k < width; k++)
                {
                    objects[i, j, k] = Instantiate(cube,
                                 new Vector3(i * 1.1f, j * 1.1f, k * 1.1f),
                                 Quaternion.identity);
                }
            }
        }

        ca = new CellularAutamata3D(lenght, height, width, rule);
        n = new NeuroNet(ref ca);
    }

    public void ButtonDraw_Click()
    {
        short i = short.Parse(inputI.text);
        short j = short.Parse(inputJ.text);
        short k = short.Parse(inputK.text);
        short val = short.Parse(inputDrawVal.text);

        ca.ChangeAge(i, j, k, val);
        DrawDot(i, j, k, val);
    }

    public void ButtonClear_Click()
    {
        generation = 0;
        run = false;

        for (short i = 0; i < lenght; i++)
        {
            for (short j = 0; j < height; j++)
            {
                for (short k = 0; k < width; k++)
                {
                    objects[i, j, k].SetActive(true);
                    objects[i, j, k].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 1.0f, 1.0f, 0f);
                    ca.ChangeAge(i, j, k, 0);
                }
            }
        }
    }
    public void ButtonDrawRnd_Click()
    {
        

        for (short i = 0; i < lenght; i++)
        {
            for (short j = 0; j < height; j++)
            {
                short k = 1;
                {
                    
                    float al = Random.Range(0f, ca.rule.max_age);
                    short val=  (short)(al);
                    
                    ca.ChangeAge(i, j, k, val);
                    DrawDot(i, j, k, val);
                }
            }
        }
    }
    public void ButtonDrawCube_Click()
    {
        

        for (short i = 0; i < lenght/2; i++)
        {
            for (short j = 0; j < height; j++)
            {
                short k = 1;
                if(j%3==1)
                {
                    ca.ChangeAge(i, j, k, 1);
                    objects[i, j, k].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0, 0, 1f);
                }
            }
        }
    }
    public void ButtonDrawSome_Click()
    {
        //run = false;
        //run_audio = false;

        for (short i = 0; i < lenght; i++)
        {
            for (short j = i; j < height; j+=2)
            {
                short k = 1; 
                {
                    ca.ChangeAge(i, j, k, 1);
                    objects[i, j, k].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0, 0, 1f);
                }
            }
        }
    }

    
    public void ButtonGetStopMic_Click()
    {
        run_audio = !run_audio;

        if (run_audio)
        {
            audioSource = GetComponents<AudioSource>()[0];
            

            audioSource.clip = Microphone.Start(dropdown.options[dropdown.value].text, true, 10, 44100);
            audioSource.loop = true;
            while (!(Microphone.GetPosition(dropdown.options[dropdown.value].text) > 0)) { }

            dropdown.enabled = false;
        }
        else
        {
            audioSource.Stop();
            Microphone.End(dropdown.options[dropdown.value].text);

            dropdown.enabled = true;
            lastSample = 0;
        }
    }

    public void inputToggle_toggle()
    {
        if (!inputToggle.isOn)
        {
            for(int i=0;i<lenght;i++)
                for (int j = 0; j < height; j++)
                    for (int k = 0; k < width; k++)
                    {
                        objects[i, j, k].SetActive(false);
                    }
        }
    }

    public void ButtonSleep_Click()
    {
        if (run) ButtonRunStop_Click();
        if (run_audio) ButtonGetStopMic_Click();

        n.Sleep();
    }

    public void ButtonWake_Click()
    {
        if (run) ButtonRunStop_Click();
        if (run_audio) ButtonGetStopMic_Click();

        NeuroNet nnn = DNA.Wake();
        n = nnn;
        ca = n.ca;

        foreach (GameObject o in objects) Destroy(o);

        lenght = ca.lenght;
        height = ca.height;
        width = ca.width;
        rule = ca.rule.rule;

        objects = new GameObject[lenght, height, width];
        for (int i = 0; i < lenght; i++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int k = 0; k < width; k++)
                {
                    objects[i, j, k] = Instantiate(cube,
                                 new Vector3(i * 1.1f, j * 1.1f, k * 1.1f),
                                 Quaternion.identity);
                }
            }
        }

    }

    private void DrawDot(short i,short j,short k, short val)
    {
        if (!inputToggle.isOn) return;

        if (val > 0)
        {
            objects[i, j, k].SetActive(true);
            float al = 1.0f / ca.rule.max_age * ((ca.rule.max_age + 1.0f) - val);
            objects[i, j, k].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 1f - al, 1f - al, al);
        }
        else
        {
            //objects[i, j, k].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 1.0f, 1.0f, 0f);
            objects[i, j, k].SetActive(false);
        }
    }


    int lastSample;
    float Hz = 0;
    const int BUF_COMPLEX = 1024;
    private void DrawAudio()
    {
        if (run_audio)
        {
            
            int pos = Microphone.GetPosition(dropdown.options[dropdown.value].text);
            int diff = pos - lastSample;
            if (diff > 0)
            {
                float[] samples = new float[diff * audioSource.clip.channels];
                audioSource.clip.GetData(samples, lastSample);
                lenght_mic_buffer = samples.Length;

                Complex[] spec2 = new Complex[BUF_COMPLEX];

                int mm = 0;
                for (short i = 0; i < (lenght / 2) && mm < lenght_mic_buffer; i++)//на половине рисуем сырое аудио
                {
                    for (short j = 0; j < height && mm < lenght_mic_buffer; j++)
                    {
                        //приводим float от -1 до 1 к виду 
                        short b = (short)(samples[mm] * ca.rule.max_age * 6);//потому что в 6 клетках 1 флоат рисуем

                        if (b == 0)
                        {
                            //рисуем ноль 
                            ca.ChangeAgeFast(i, j, 0, 0);
                            //DrawDot(i, j, 0, 0);
                            //рисуем ноль на противоположной стенке
                            ca.ChangeAgeFast(i, j, (short)(width - 1), 0);
                            //DrawDot(i, j, (short)(width - 1), 0);
                        }
                        else if (b > 0)//положительные значения рисуются ни главной стенке
                            for (short k = 0; k < 6; k++)
                            {
                                //рисуем ноль на противоположной стенке
                                ca.ChangeAgeFast(i, j, (short)(width - 1 - k), 0);
                                //DrawDot(i, j, (short)(width - 1 - k), 0);
                                if (b == 0)
                                {
                                    ca.ChangeAgeFast(i, j, k, 0);
                                    break;
                                }
                                b -= ca.rule.max_age;
                                if (b < 0)
                                {
                                    ca.ChangeAgeFast(i, j, k, (short)(-b));
                                    break;
                                }
                                else
                                {
                                    ca.ChangeAgeFast(i, j, k, 1);
                                }
                            }

                        else //отрицательные значения рисуются на противоположной стенке
                        {
                            b = (short)-b;
                            for (short k = (short)(width - 1); k > width - 1 - 6; k--)
                            {
                                //рисуем ноль на первой стенке
                                ca.ChangeAgeFast(i, j, (short)(width - 1 - k), 0);
                                //DrawDot(i, j, (short)(width - 1 - k), 0);
                                if (b == 0)
                                {
                                    ca.ChangeAgeFast(i, j, k, 0);
                                    //DrawDot(i, j, k, 0);

                                    break;
                                }
                                b -= ca.rule.max_age;
                                if (b < 0)
                                {
                                    ca.ChangeAgeFast(i, j, k, (short)(-b));
                                    //DrawDot(i, j, k, (short)(-b));
                                    break;
                                }
                                else
                                {
                                    ca.ChangeAgeFast(i, j, k, 1);
                                    //DrawDot(i, j, k, 1);
                                }
                            }

                        }
                        //для fft
                        if(mm< BUF_COMPLEX) spec2[mm] = new Complex(samples[mm], 0);

                        mm++;

                        

                    }
                }

                //рисуем FFT
                FFT.CalculateFFT(spec2, false);

                mm = 0;
                int maxn = 0;
                float magnmax = 0;
                    
                for (short i = (short)(lenght / 2-3); i >=0  && mm < BUF_COMPLEX/2; i-=3)//на второй половине рисуем ФФТ
                {
                    for (short j = 0; j < height && mm < BUF_COMPLEX/2; j++)
                    {
                        short v = (short)(ca.rule.max_age+2 - spec2[mm].magnitude* ca.rule.max_age*456);

                        //яркие сигналы с пердыдущего цикла сотрем
                        ca.ChangeAgeFast((short)(i-1 + lenght / 2), j, 0, 0);
                        ca.ChangeAgeFast((short)(i-1 + lenght / 2), j, (short)(width - 1), 0);
                        ca.ChangeAgeFast((short)(i-2 + lenght / 2), j, 0, 0);
                        ca.ChangeAgeFast((short)(i-2 + lenght / 2), j, (short)(width - 1), 0);

                        if (v <= 1)//очень яркий сигнал
                        {
                            switch (v)
                            {
                                case 1:
                                    ca.ChangeAge((short)(i - 1 + lenght / 2), j, 0, (short)(ca.rule.max_age));
                                    ca.ChangeAge((short)(i - 1 + lenght / 2), j, (short)(width - 1), (short)(ca.rule.max_age));
                                    break;
                                case short n when (n < 1 && n>-15):
                                    ca.ChangeAge((short)(i - 1 + lenght / 2), j, 0, (short)(ca.rule.max_age/2));
                                    ca.ChangeAge((short)(i - 1 + lenght / 2), j, (short)(width - 1), (short)(ca.rule.max_age/2));
                                    break;
                                case short n when (n <= -15 && n > -30):
                                    ca.ChangeAge((short)(i - 1 + lenght / 2), j, 0, (short)(1));
                                    ca.ChangeAge((short)(i - 1 + lenght / 2), j, (short)(width - 1), (short)(1));
                                    break;
                                case short n when (n <= -30 && n > -45):
                                    ca.ChangeAge((short)(i - 1 + lenght / 2), j, 0, (short)(1));
                                    ca.ChangeAge((short)(i - 1 + lenght / 2), j, (short)(width - 1), (short)(1));
                                    ca.ChangeAge((short)(i - 2 + lenght / 2), j, 0, (short)(ca.rule.max_age));
                                    ca.ChangeAge((short)(i - 2 + lenght / 2), j, (short)(width - 1), (short)(ca.rule.max_age));
                                    break;
                                case short n when (n <= -45 && n>-60):
                                    ca.ChangeAge((short)(i - 1 + lenght / 2), j, 0, (short)(1));
                                    ca.ChangeAge((short)(i - 1 + lenght / 2), j, (short)(width - 1), (short)(1));
                                    ca.ChangeAge((short)(i - 2 + lenght / 2), j, 0, (short)(ca.rule.max_age/2));
                                    ca.ChangeAge((short)(i - 2 + lenght / 2), j, (short)(width - 1), (short)(ca.rule.max_age/2));
                                    break;
                                default:
                                    ca.ChangeAge((short)(i - 1 + lenght / 2), j, 0, (short)(1));
                                    ca.ChangeAge((short)(i - 1 + lenght / 2), j, (short)(width - 1), (short)(1));
                                    ca.ChangeAge((short)(i - 2 + lenght / 2), j, 0, (short)(1));
                                    ca.ChangeAge((short)(i - 2 + lenght / 2), j, (short)(width - 1), (short)(1));
                                    break;
                            }
                            v = 1;
                        }
                        else if (v > ca.rule.max_age) v = 0;

                        if(v==0)
                        {
                            ca.ChangeAgeFast((short)(i + lenght / 2), j, 0, v);
                            ca.ChangeAgeFast((short)(i + lenght / 2), j, (short)(width - 1), v);
                        }
                        else //а вот ненулевые значения лучше чтобы попали в КА сразу и быстро
                        {
                            ca.ChangeAge((short)(i + lenght / 2), j, 0, v);
                            ca.ChangeAge((short)(i + lenght / 2), j, (short)(width - 1), v);
                        }

                        if ((float)spec2[mm].magnitude > magnmax)
                        {
                            maxn = mm;
                            magnmax =(float) spec2[mm].magnitude;
                        }

                        mm++;
                    }
                        
                }
                Hz = maxn * AudioSettings.outputSampleRate / BUF_COMPLEX;
            }
            lastSample = pos;
        }
    }

    
    public byte[] ToByteArray(float[] floatArray)
    {
        int len = floatArray.Length * 4;
        byte[] byteArray = new byte[len];
        int pos = 0;
        foreach (float f in floatArray)
        {
            byte[] data = System.BitConverter.GetBytes(f);
            System.Array.Copy(data, 0, byteArray, pos, 4);
            pos += 4;
        }
        return byteArray;
    }

    public void Add10SumNrns_click()
    {
        if (run) ButtonRunStop_Click();
        if (run_audio) ButtonGetStopMic_Click();

        n.AddSomeNeuronRandom(10, 's');
    }
    public void Add10StructNrns_click()
    {
        if (run) ButtonRunStop_Click();
        if (run_audio) ButtonGetStopMic_Click();

        n.AddSomeNeuronRandom(10, 'c');
    }

}
