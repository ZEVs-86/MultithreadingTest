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

namespace MultithreadingTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModel();
        }
    }

    /// <summary>
    /// Button's actions from XAML handler
    /// </summary>
    public class CommandHandler : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private Action action;
        private bool canExecute;

        public CommandHandler(Action action, bool canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return canExecute;
        }

        public void Execute(object parameter)
        {
            action();
        }
    }

    /// <summary>
    /// Button's actions and multithreading work event handlers
    /// </summary>
    public class ViewModel : DependencyObject
    {
        // private ICommand click;
        private bool canExecute;
        private Worker worker;
        private SynchronizationContext context;

        public DependencyProperty StartIsEnabledProperty;
        public DependencyProperty ProgressProperty;

        public ViewModel()
        {
            canExecute = true;

            StartIsEnabledProperty = DependencyProperty.Register("StartIsEnabled", typeof(bool), typeof(ViewModel));
            ProgressProperty = DependencyProperty.Register("Progress", typeof(int), typeof(ViewModel));

            context = SynchronizationContext.Current;

            StartIsEnabled = true;
        }

        // click from XAML
        public ICommand ClickCommandStart
        {
            get { return new CommandHandler(() => ActionStart(), canExecute); }            
        }
        public ICommand ClickCommandCancel
        {
            get { return new CommandHandler(() => ActionCancel(), canExecute); }
        }

        // click start action (first thread)
        public void ActionStart()
        {
            Console.WriteLine("Action Start");

            // TODO: how to interact with UI elements?
            StartIsEnabled = false;

            worker = new Worker();
            worker.ProcessAdvanced += Worker_ProcessAdvanced;
            worker.WorkComplete += Worker_WorkComplete;

            Thread thread = new Thread(worker.Work);
            thread.Start(context);
        }

        // click cancel action (first thread)
        public void ActionCancel()
        {
            StartIsEnabled = true;
            worker.Cancel();
            Console.WriteLine("Action Cancel");
        }

        // properties to interact with XAML
        public bool StartIsEnabled
        {
            get { return (bool)GetValue(StartIsEnabledProperty); }
            set { SetValue(StartIsEnabledProperty, value); }
        }
        public int Progress
        {
            get { return (int)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value * 10); }
        }
        
        // event handler (executes in second thread)
        private void Worker_WorkComplete(bool cancelled)
        {
            StartIsEnabled = true;

            if (cancelled)
                Progress = 0;

            if (cancelled)
                Console.WriteLine("Work cancelled!");
            else
                Console.WriteLine("Work done.");
        }

        // event handler (starts from second thread)
        private void Worker_ProcessAdvanced(int progress)
        {
            Progress = progress + 1;    // dirty progressbar delay hack
            Progress = progress;
            Console.WriteLine("Progress handled " + progress.ToString());            
        }
    }
    

    /// <summary>
    /// Slow methods that shoud be started asynchronously
    /// </summary>
    public class Worker
    {
        private bool cancelled = false;

        public void Work(object param)
        {
            SynchronizationContext context = (SynchronizationContext) param;

            for (int i = 1; i <= 10; i++)
            {
                if (cancelled)
                    break;

                Console.WriteLine("Working...");
                Thread.Sleep(200);

                // send event abount progress
                context.Send(OnProgressAdvanced, i);
            }

            // send event about work done
            context.Send(OnWorkComplete, cancelled);
        }

        public void Cancel()
        {
            cancelled = true;
            Console.WriteLine("Work cancelled!");
        }

        public event Action<int> ProcessAdvanced;

        public event Action<bool> WorkComplete;

        // worker actions wrapper voids for sync.context's call
        public void OnProgressAdvanced(object progress)
        {
            ProcessAdvanced((int)progress);
        }
        public void OnWorkComplete(object cancelled)
        {
            WorkComplete((bool) cancelled);
        }

    }

}
