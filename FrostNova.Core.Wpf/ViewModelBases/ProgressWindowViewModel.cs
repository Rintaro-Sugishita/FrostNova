using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FrostNova.Core.Wpf.ViewModelBases
{
    public partial class ProgressWindowViewModel : DialogWindowViewModelBase
    {

        [ObservableProperty]
        private Func<ProgressInfo, Task>? _action;

        internal Progress<ProgressInfo> _progress = new Progress<ProgressInfo>();

        [ObservableProperty]
        private ProgressInfo _progressInfo;

        [ObservableProperty]
        [property: NotifyPropertyChangedFor(nameof(CanCancel))]
        private CancellationTokenSource? _cancellationToken;

        [ObservableProperty]
        private Exception? _exception;

        /// <summary>
        /// キャンセル押下時に、協調キャンセルに続けて呼ばれる強制終了処理。
        /// 応答しない外部プロセスを Kill する等に用いる。
        /// </summary>
        public Action? ForceCancelAction { get; set; }

        /// <summary>
        /// 実処理タスク。設定しておくと、完了時にウィンドウを自動で閉じる。
        /// </summary>
        public Task? CompletionTask { get; set; }

        /// <summary>
        /// ウィンドウの不透明度。既定は 0(不可視)。<see cref="ShowDelayMs"/> を超えて処理が
        /// 続いたときだけ 1(可視)にし、短い処理ではダイアログを見せない。
        /// </summary>
        [ObservableProperty]
        private double _windowOpacity = 0.0;

        /// <summary>この時間(ms)を超えて実行中のときだけウィンドウを可視化する(チラつき防止)。</summary>
        public int ShowDelayMs { get; set; } = 1000;

        /// <summary>一度可視化したら、最低この時間(ms)は表示し続けてから閉じる(フラッシュ防止)。</summary>
        public int MinVisibleMs { get; set; } = 500;

        private DateTime _revealedAtUtc;
        private DispatcherTimer? _revealTimer;


        [RelayCommand(CanExecute = nameof(CanCancel))]
        private void Cancel()
        {
            CancellationToken?.Cancel();
            ForceCancelAction?.Invoke();
        }

        public bool CanCancel { get => CancellationToken != null; }


        public ProgressWindowViewModel()
        {
            _progressInfo = new ProgressInfo(_progress);
        }

        protected internal override void OnLoaded()
        {
            base.OnLoaded();

            var task = CompletionTask;

            // 既に完了しているなら、可視化せずそのまま閉じる。
            if (task != null && task.IsCompleted)
            {
                CloseOnUi();
                return;
            }

            // ShowDelayMs 経過してもまだ実行中なら、そのとき初めて姿を現す。
            _revealTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(0, ShowDelayMs)) };
            _revealTimer.Tick += (_, _) =>
            {
                _revealTimer!.Stop();
                var t = CompletionTask;
                if (t == null || !t.IsCompleted)
                {
                    _revealedAtUtc = DateTime.UtcNow;
                    WindowOpacity = 1.0;
                }
            };
            _revealTimer.Start();

            task?.ContinueWith(_ => OnWorkCompleted(), TaskScheduler.Default);
        }

        private void OnWorkCompleted()
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                Close();
                return;
            }

            dispatcher.BeginInvoke(new Action(() =>
            {
                _revealTimer?.Stop();

                // 既に可視化済みなら、最低表示時間を確保してから閉じてフラッシュを防ぐ。
                if (WindowOpacity > 0.0)
                {
                    var elapsed = DateTime.UtcNow - _revealedAtUtc;
                    var min = TimeSpan.FromMilliseconds(Math.Max(0, MinVisibleMs));
                    if (elapsed < min)
                    {
                        var closeTimer = new DispatcherTimer { Interval = min - elapsed };
                        closeTimer.Tick += (s, _) =>
                        {
                            ((DispatcherTimer)s!).Stop();
                            Close();
                        };
                        closeTimer.Start();
                        return;
                    }
                }

                Close();
            }));
        }

        private void CloseOnUi()
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                Close();
                return;
            }

            dispatcher.BeginInvoke(new Action(Close));
        }


        public DispatcherOperation RunTask()
        {
            //Exception? innerException = null;

            //await Task.Run(async () =>
            //{
            //    try
            //    {
            //        if (Action != null)
            //        {
            //            //重い処理は await Taskで実行するとUIを固めずに処理できる
            //            //UI更新はProgressのReportからする
            //            await Action.Invoke(ProgressInfo);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Exception = ex;
            //    }
            //    finally
            //    {
            //        Application.Current.Dispatcher.Invoke(() =>
            //        {
            //            CloseCommandExecute();
            //        });
            //    }

            //});
            return Application.Current.Dispatcher.InvokeAsync(() =>
              {
                  try
                  {
                      if (Action != null)
                      {
                          //重い処理は await Taskで実行するとUIを固めずに処理できる
                          //UI更新はProgressのReportからする
                          Action.Invoke(ProgressInfo);
                      }
                  }
                  catch (Exception ex)
                  {
                      Exception = ex;
                  }
                  finally
                  {
                      Close();
                  }

              });
        }




    }
}
