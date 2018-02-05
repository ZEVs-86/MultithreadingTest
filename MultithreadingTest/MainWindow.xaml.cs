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
    /// Button's
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
    /// Button's actions
    /// </summary>
    public class ViewModel : DependencyObject
    {
        // private ICommand click;
        private bool canExecute;

        private Worker worker;

        private SynchronizationContext context;

        public DependencyProperty StartIsEnabledProperty;


        public ViewModel()
        {
            canExecute = true;
            StartIsEnabledProperty = DependencyProperty.Register("StartIsEnabled", typeof(bool), typeof(ViewModel));

            StartIsEnabled = true;

            context = SynchronizationContext.Current;
        }

        public ICommand ClickCommandStart
        {
            get
            {
                return new CommandHandler(() => ActionStart(), canExecute);
            }
        }
        public ICommand ClickCommandCancel
        {
            get
            {
                return new CommandHandler(() => ActionCancel(), canExecute);
            }
        }

        public bool StartIsEnabled
        {
            get
            {
                Console.WriteLine("get property: " + StartIsEnabledProperty.ToString());
                return (bool)GetValue(StartIsEnabledProperty);
            }
            set
            {
                Console.WriteLine("set property: " + value.ToString());
                SetValue(StartIsEnabledProperty, value);
            }
        }

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

        private void Worker_WorkComplete(bool cancelled)
        {
            if (cancelled)
                Console.WriteLine("Work cancelled!");
            else
                Console.WriteLine("Work done.");
        }

        public void ActionCancel()
        {
            Console.WriteLine("Action Cancel");
        }

        private void Worker_ProcessAdvanced(int progress)
        {
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

            for (int i = 0; i < 10; i++)
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
