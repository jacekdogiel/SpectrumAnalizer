using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using NAudio.Wave;
using MathNet.Numerics.Transformations;
using NationalInstruments.DAQmx;


namespace Program_do_analizy_widma
{
    public partial class Form1 : Form
    {
        private Task myTask;
        private AnalogMultiChannelReader reader;
        private double[,] data;
        private DataTable dataTable = null;
        //zmienna do warunków związanych z NI
        Boolean ni;
        //timer do dlugosci nagrywania dzwieku
        Timer timer = new Timer();
        //tablica przechowująca widmo amplitudowe
        double[] amp = new double[500000];
        //zmienna przechowująca wartość sygnału
        double[] s = new double[500000];
        WaveIn waveInStream;
        WaveFileWriter writer;
        //zmienna przemnażania liczby próbek dźwieku
        int mnoznik;
        //1s nagrania
        int a = 1;
        //2s nagrania
        int b = 2;
        //4s nagrania
        int c = 4;
        //8s nagrania
        int d = 8;
        //16s nagrania
        int e = 16;
        //zmienna do warunku poruszania myszką
        Boolean move = false;
        //liczba próbek z 
        int samples;
        //tablica przechowująca realis
        double[] real;
        //tablica przechowująca imaginalis
        double[] imag;
        public Form1()
        {
            InitializeComponent();
            button1.Enabled = false;
            button5.Enabled = false;
            comboBox1.Items.Add(a);
            comboBox1.Items.Add(b);
            comboBox1.Items.Add(c);
            comboBox1.Items.Add(d);
            comboBox1.Items.Add(e);
            comboBox1.SelectedIndex = 0;
            this.WindowState = FormWindowState.Maximized;
            this.Show();
            //sluchacz do ruszania scrollem
            this.MouseWheel += new MouseEventHandler(Form1_MouseWheel);
            dataTable = new DataTable();
            //dodanie kanałów dostepnych urządzeń NI
            physicalChannelComboBox.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            if (physicalChannelComboBox.Items.Count > 0)
                physicalChannelComboBox.SelectedIndex = 0;
        }
        //funkcja sprawdzająca czy wystąpił ruch nad chart2
        private void chart2_MouseMove(object sender, MouseEventArgs e)
        {
            move=true;
        }
        //funkcja sprawdzająca czy myszka opuściła chart2
        private void chart2_MouseLeave(object sender, EventArgs e)
        {
            move = false;
        }
        //funkcja słuchacza scrolla
        void Form1_MouseWheel(object sender, MouseEventArgs e)
	    {
            if (move)
            {
                if (e.Delta > 0)
                {
                    hScrollBar1.Maximum += 1;
                    if (hScrollBar1.Maximum <= 0)
                    {
                        hScrollBar1.Maximum = 1;
                    }
                    if (hScrollBar1.Maximum >= 0)
                    {
                        wykresl2();
                    }
                }
                else
                {
                    hScrollBar1.Maximum -= 1;
                    if (hScrollBar1.Maximum <= 0)
                    {
                        hScrollBar1.Maximum = 1;
                    }
                    if (hScrollBar1.Maximum >= 0)
                    {
                        wykresl2();
                    }
                }
            }
	    }
        //rysowanie wykresu widma chart1
        private void wykresl()
        {
            //czestotliwosc próbkowania dla urządzenia NI wybrana przez użytkownika
            int niRate = (int)rateNumeric.Value/2;
            //czestotliwość widma dźwięku
            int rate = 4000;
            //mnożnik do liczenia liczby próbek
            double mnoznik2 = 1;
            if (ni)
            {
                rate = niRate;
            }
            //zmienna dla osi X wykresu
            double h = 0;
            //skok o jaki ma sie zmieniac oś X
            double dziel = 1.0 / mnoznik;
            if (ni)
            {
                //skok o jaki ma sie zmieniac oś X dla urządzenia NI
                dziel = (1.0 * niRate) / (0.5 * samples);
                //mnożnik do wyliczenia połowy próbek widma
                mnoznik2 = (1.0 * samples) / (2.0 * niRate);
            }
            chart1.ChartAreas[0].AxisX.LogarithmBase = (int)numericUpDown1.Value;
            //petla rysujaca punkty na wykresie
            for (int i = 1; i < (int)rate*mnoznik*mnoznik2; i++)
                        {
                            chart1.Series["Widmo sygnału"].Points.AddXY(h+dziel, amp[i]);
                            h+=dziel;
                        }
        }
        //funkcja uruchamiająca rysowanie wykresu
        private void button1_Click(object sender, EventArgs e)
        {
            FFT() ;
            wykresl();
            wykresl2();
            button1.Enabled = false;
            button5.Enabled = true;
        }
        //funkcja licząca Fast Fourier Transform
        private void FFT()
        {
                double log;
                samples=8192;
                mnoznik = (int)comboBox1.SelectedItem;
                if (ni)
                {
                    mnoznik = 1;
                    //metoda dostosowująca ilość próbek tak, żeby były potęgą 2, przez wydłużenie tablicy i dodanie zer
                    log = Math.Log((double)samplesPerChannelNumeric.Value, 2);
                    log = Math.Ceiling(log);
                    samples = (int)Math.Pow(2,log);
                    //
                }
                //pobranie próbek sygnału do tablicy ss
                double[] ss = new double[samples * mnoznik];
                for (int i = 0; i < samples * mnoznik; i++)
                {
                    ss[i] = s[i];
                }
                real = new double[samples * mnoznik];
                imag = new double[samples * mnoznik];
                RealFourierTransformation ft = new RealFourierTransformation();
                //policzenie transformaty
                ft.TransformForward(ss,out real,out imag);
                for (int i = 0; i < samples * mnoznik; i++)
                {
                    //policzenie widma amplitudowego przez wyciągnięcie wartości bezwzględnej z widma
                    amp[i] = Math.Sqrt(Math.Pow(real[i],2)+Math.Pow(imag[i],2));
                }             
        }
        //włączenie nagrywania dźwięku
        private void button4_Click(object sender, EventArgs e)
        {
            audio();
            button4.Enabled = false;
            startButton.Enabled = false;
        }
        //funkcja realizujaca pobieranie dźwięku z mikrofonu
        private void audio()
        {
            //zmienna ustalająca czas trwania nagrania
            mnoznik = (int)comboBox1.SelectedItem;
            waveInStream = new WaveIn();
            String relPath = System.IO.Path.Combine(Application.StartupPath, "temp.wav");
            writer = new WaveFileWriter(relPath, waveInStream.WaveFormat);
            waveInStream.DataAvailable += new EventHandler<WaveInEventArgs>(waveInStream_DataAvailable);
            waveInStream.StartRecording();
            //czas nagrania
            timer.Interval = 1000 * mnoznik + 240;
            timer.Enabled = true;
            timer.Tick += new System.EventHandler(OnTimerEvent);
        }
        //funkcja timera zatrzymująca nagrywanie
        public void OnTimerEvent(object source, EventArgs e)
        {
            audioStop();

        }
        //zapisanie pliku i wczytanie go do tablicy bajtów, następnie do tablicy s(tablica z sygnałem)
        private void audioStop()
        {
            timer.Enabled = false;
            waveInStream.StopRecording();
            waveInStream.Dispose();
            waveInStream = null;
            writer.Close();
            writer = null;

            //przecztanie wszystkich bajtów pliku wav z nagranym wcześniej dźwiękiem
            String relPath = System.IO.Path.Combine(Application.StartupPath, "temp.wav");
            byte[] bytes = System.IO.File.ReadAllBytes(relPath);
            int h = 0;
            for (int i = 44; i < 16384 * mnoznik; i += 2)
            {
                //przetworzenie 16 bitów na int
                s[h] = Convert.ToDouble(BitConverter.ToInt16(bytes, i));
                h++;
            }
            button1.Enabled = true;
        }
        void waveInStream_DataAvailable(object sender, WaveInEventArgs e)
        {
            writer.WriteData(e.Buffer, 0, e.BytesRecorded);
        }

        //funkcja wywoływana przesuwaniem paska przewijania w bok
        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            wykresl2();
        }
        //rysowanie wykresu chart2
        private void wykresl2()
        {
            int niRate = (int)rateNumeric.Value / 2;
            int rate = 4000;
            double mnoznik2 = 1;
            if (ni)
            {
                rate = niRate;
            }
            double h = 0;
            double dziel = 1.0 / mnoznik;
            if (ni)
            {
                dziel = (1.0 * niRate) / (0.5 * samples);
                mnoznik2 = (1.0 * samples) / (2.0 * niRate);
            }
            int value, samples2;
            value = hScrollBar1.Value;
            samples2 = (int)((rate * mnoznik *mnoznik2) / (hScrollBar1.Maximum + 1));
            this.chart2.Series["Widmo sygnału"].Points.Clear();
            for (int i = (int)samples2 * value; i < (int)samples2 * (value + 1); i++)
            {
                chart2.Series["Widmo sygnału"].Points.AddXY(rate /(hScrollBar1.Maximum + 1)*value + h+dziel, amp[i+1]);
                h+=dziel;
            }
        }
        //nowy pomiar - restart programu
        private void button2_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }
        //komunikacja z urządzeniem National Instruments
        private void startButton_Click(object sender, System.EventArgs e)
        {
            //wyłącz przycisk pobierania próbek
            startButton.Enabled = false;
            //wyłącz przycisk pobierania dżwięku
            button4.Enabled = false;
            //zmienna do warunków programu dla urządzenia NI-USB
            ni = true;
            try
            {
                //utworzenie nowego zadania pobierającego próbki
                myTask = new Task();

                //inicjalizacja zmiennych lokalnych

                //częstotliwość próbkowania
                double sampleRate = Convert.ToDouble(rateNumeric.Value);
                //minimalna wartość próbki sygnału wyrażona w Voltach
                double rangeMinimum = Convert.ToDouble(minimumValueNumeric.Value);
                //maksymalna wartość próbki sygnału wyrażona w Voltach
                double rangeMaximum = Convert.ToDouble(maximumValueNumeric.Value);
                //ilość próbek do pobrania na kanał
                int samplesPerChannel = Convert.ToInt32(samplesPerChannelNumeric.Value);

                //utworzenie kanału do pobierania próbek
                myTask.AIChannels.CreateVoltageChannel(physicalChannelComboBox.Text, "",
                    (AITerminalConfiguration)(-1), rangeMinimum, rangeMaximum, AIVoltageUnits.Volts);

                //konfiguracja specyfikacji czasowych    
                myTask.Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising,
                    SampleQuantityMode.FiniteSamples, samplesPerChannel);

                //weryfikacja zadania
                myTask.Control(TaskAction.Verify);

                reader = new AnalogMultiChannelReader(myTask.Stream);
                reader.BeginReadMultiSample(samplesPerChannel, new AsyncCallback(myCallback), null);
            }
            catch (DaqException exception)
            {
                MessageBox.Show(exception.Message);
                startButton.Enabled = true;
                myTask.Dispose();
            }
        }

        private void myCallback(IAsyncResult ar)
        {
            try
            {
                //pobranie dostępnych danych z kanału urządzenia
                data = reader.EndReadMultiSample(ar);

                //pobranie danych do tabeli
                dataToDataTable(data, ref dataTable);

            }
            catch (DaqException exception)
            {
                MessageBox.Show(exception.Message);
            }
            finally
            {
                myTask.Dispose();
                //włącz przycisk rysowania wykresu
                button1.Enabled = true;
            }
        }
        //pobranie ustalonej liczby próbek w ustalonej częstotliwości do tabeli z sygnałem
        private void dataToDataTable(double[,] sourceArray, ref DataTable dataTable)
        {
            try
            {
                //liczba próbek
                int numOfSamples = Convert.ToInt32(samplesPerChannelNumeric.Value);
                int channelCount = sourceArray.GetLength(0);
                int dataCount = (sourceArray.GetLength(1) < numOfSamples) ? sourceArray.GetLength(1) : numOfSamples;
                //przeniesienie próbek do tablicy double s[]
                for (int currentDataIndex = 0; currentDataIndex < dataCount; currentDataIndex++)
                {
                    for (int currentChannelIndex = 0; currentChannelIndex < channelCount; currentChannelIndex++)
                        s[currentDataIndex] = (double)sourceArray.GetValue(currentChannelIndex, currentDataIndex);
                }
                

            }
            catch (Exception e)
            {
                MessageBox.Show(e.TargetSite.ToString());
                myTask.Dispose();
                //włącz przycsik pobierania próbek
                startButton.Enabled = true;
            }
        }
        //sprawdzenie zmiany wartości podstawy log i wyświetlenie wykresu na nowo
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            wykresl();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            int rozmiar = real.Length;
            double[] signal= new double[rozmiar];
            double[] nreal = new double[rozmiar];
            double[] nimag = new double[rozmiar];
            double mnoznik2=1;
            if (ni)
            {
                mnoznik2=((double)(rozmiar)*1.0)/((double)(rateNumeric.Value)*1.0);
            }
            for (int i = (int)Math.Ceiling((double)numericUpDown2.Value * mnoznik2 * mnoznik); i < (int)Math.Ceiling((double)numericUpDown3.Value * mnoznik2 * mnoznik); i++)
            {
                nreal[i] = real[i];
                nimag[i] = imag[i];
            }
            for (int i = rozmiar - (int)Math.Ceiling((double)numericUpDown3.Value * mnoznik2 * mnoznik); i < rozmiar - (int)Math.Ceiling((double)numericUpDown2.Value * mnoznik2 * mnoznik); i++)
            {
                nreal[i] = real[i];
                nimag[i] = imag[i];
            }
            nreal[0] = real[0];
            nimag[0] = imag[0];
            RealFourierTransformation ft = new RealFourierTransformation();
            ft.TransformBackward(nreal,nimag,out signal);
            if (ni)
            {
                double h = 0;
                double dziel = 1.0 / (int)rateNumeric.Value;
                for (int i = 0; i < (int)samplesPerChannelNumeric.Value; i++)
                {
                    chart3.Series["Sygnał"].Points.AddXY(h + dziel, signal[i]);
                    h += dziel;
                }
            }
            else
            {
                double g = 0;
                double dziel2 = 1.0 / 8000.0;
                for (int i = 0; i < 8192 * mnoznik; i++)
                {
                    chart3.Series["Sygnał"].Points.AddXY(g + dziel2, signal[i]);
                    g += dziel2;
                }
            }
            button5.Enabled = false;
        }
        //zapisanie obrazu wykresu w zadanej lokalizacji po kliknięciu na wykres
        private void chart1_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                chart1.SaveImage(saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        //zapisanie obrazu wykresu w zadanej lokalizacji po kliknięciu na wykres
        private void chart3_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                chart3.SaveImage(saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        //zapisanie obrazu wykresu w zadanej lokalizacji po kliknięciu na wykres
        private void chart2_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                chart2.SaveImage(saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        //zmiana skali na logarytmiczną
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
             chart1.ChartAreas[0].AxisX.IsLogarithmic = true;
             wykresl();
        }
        //zmiana skali na linjową
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            chart1.ChartAreas[0].AxisX.IsLogarithmic = false;
            wykresl();
        }

    }

}


