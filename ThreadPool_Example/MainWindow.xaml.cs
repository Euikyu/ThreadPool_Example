using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ThreadPool_Example
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public RegisteredWaitHandle m_WaitHandle;
        public Thread m_ThreadWork;
        public object m_LockInspectData = new Object();
        public Queue<int> m_InspectData = new Queue<int>();
        //set 시키면 사용 중인 스레드 중 하나만 알아서 이벤트 발생
        public AutoResetEvent m_NewInspectData = new AutoResetEvent(false);
        public MainWindow()
        {
            InitializeComponent();
            //스레드 16개 미리 생성
            ThreadPool.SetMinThreads(16, 16);
        }


        public void Proc(object seq, bool timedOut)
        {
            //제한시간동안 일 배정 안됐을 때
            if (timedOut) return;

            //일 배정 됐을 때
            Inspect(seq);
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

            while (true)
            {
                // 1초동안 CPU 처묵 처묵
                TimeSpan Elapse = DateTime.Now - Start;

                if (Elapse.TotalMilliseconds > 1000)
                    break;
            }

        }
        
        public void GenerateWork()
        {
            int i = 0;
            while (true)
            {
                // 일꺼리 생성
                lock (this.m_LockInspectData)
                {
                    int Job = i;
                    this.m_InspectData.Enqueue(Job);
                }

                // 일꺼리 있으니깐 일 해라~
                m_NewInspectData.Set();

                i++;
                Thread.Sleep(250);
            }
        }
        
        private void Run_Click(object sender, RoutedEventArgs e)
        {
            if (m_ThreadWork != null && m_ThreadWork.IsAlive) return;
            int Num = int.Parse(this.textBox1.Text);
            ThreadPool.SetMaxThreads(Num, Num);
            m_WaitHandle = ThreadPool.RegisterWaitForSingleObject(m_NewInspectData, Proc, new object(), 100, false);

            // 일꺼리 만들놈
            this.m_ThreadWork = new Thread(GenerateWork);
            this.m_ThreadWork.Start();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (m_ThreadWork != null && m_ThreadWork.IsAlive)
            {
                m_WaitHandle.Unregister(m_NewInspectData);
                m_ThreadWork.Abort();
                m_ThreadWork.Join();
            }
        }
    }
}
