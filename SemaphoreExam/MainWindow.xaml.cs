using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace SemaphoreExam
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        //set 시키면 사용 중인 모든 스레드에서 이벤트 발생
        public ManualResetEvent m_Terminate = new ManualResetEvent(false);
        public Thread m_ThreadWork;
        public List<Thread> m_ThreadList;
        public Semaphore m_Semaphore;


        public object m_LockInspectData = new Object(); 
        public Queue<int> m_InspectData = new Queue<int>();
        //set 시키면 사용 중인 스레드 중 하나만 알아서 이벤트 발생
        public AutoResetEvent m_NewInspectData = new AutoResetEvent(false);

        public MainWindow()
        {
            InitializeComponent();
            this.m_ThreadList = new List<Thread>(); 
        }


        // 특정 개수의 쓰레드만 일 시키기
        public void Proc(object seq)
        {
            while (true)
            {
                // 종료 조건
                if (this.m_Terminate.WaitOne(0))
                    break;

                // 일꺼리 있을때 쓰레드에게 업무 분담
                if (!this.m_NewInspectData.WaitOne(100))
                    continue;

                // 제가 할까유?
                this.m_Semaphore.WaitOne();

                // 검사
                Inspect(seq);

                this.m_Semaphore.Release();
            }
        }

        public void Inspect(object seq)
        {
            // 일꺼리 가지고 오기
            int Job = 0;
            lock (this.m_LockInspectData)
            {
                if (this.m_InspectData.Count < 1)
                {
                    System.Diagnostics.Debug.WriteLine("Thread : " + seq.ToString() + " no job");
                    return;
                }

                Job = this.m_InspectData.Dequeue();
            }

            DateTime Start = DateTime.Now;
            System.Diagnostics.Debug.WriteLine("Thread : " + seq.ToString() + ", Job : " + Job.ToString());

            while(true)
            {
                // 종료 조건
                if (this.m_Terminate.WaitOne(0))
                    break;

                // 1초동안 CPU 처묵 처묵
                TimeSpan Elapse = DateTime.Now - Start;

                if (Elapse.TotalMilliseconds > 1000)
                    break;
            }
            
        }

        public void GenerateWork()
        {
            // 1초에 한번 일꺼리 생성
            int i = 0;
            while (true)
            {
                // 종료 조건 + 1000 ms에 한번씩 일꺼리 생성
                if (this.m_Terminate.WaitOne(250))
                    break;

                // 일꺼리 생성
                lock(this.m_LockInspectData)
                {
                    int Job = i;
                    this.m_InspectData.Enqueue(Job);
                }

                // 일꺼리 있으니깐 일 해라~
                this.m_NewInspectData.Set();

                i++;
            }
        }


        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (this.m_ThreadList.Count > 0)
                return;

            int Num = int.Parse(this.textBox1.Text);

            // 4개중 2개만 돌아라~
            this.m_Semaphore = new Semaphore(Num, 16);

            // 쓰레드 종료 리셋
            this.m_Terminate.Reset();

            for (int i=0; i<16; i++)
            {
                this.m_ThreadList.Add(new Thread(Proc));
                this.m_ThreadList[i].Start(i);
            }

            // 일꺼리 만들놈
            this.m_ThreadWork = new Thread(GenerateWork);
            this.m_ThreadWork.Start();

        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            this.m_Terminate.Set();

            // 일 만드는 넘 종료 대기
            this.m_ThreadWork.Join();

            // 쓰레드 개수 만큼 종료 대기
            foreach (Thread thread in this.m_ThreadList)
            {
                thread.Join();
            }
            this.m_ThreadList.Clear();
        }
    }
}
