using CronExpressionDescriptor;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace WeChatHelper
{
    public enum TaskStateEnum
    {
        WAITED,
        EXCUTING,
        FINISHED,
        WECHAT_ERROR,
        SYSTEM_ERROR,
        INTERNAL_ERROR,
    };

    public class SendTaskJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            return Task.Factory.StartNew(() =>
            {
                SendTask sendTask = (SendTask)context.JobDetail.JobDataMap["sendTask"];
                SystemConfig sysConfig = (SystemConfig)context.JobDetail.JobDataMap["sysConfig"];
                if (sendTask == null || sysConfig == null)
                {
                    sendTask.State = TaskStateEnum.INTERNAL_ERROR;
                    return;
                }
                if (NativeMethods.IsWorkstationLocked())
                {
                    sendTask.State = TaskStateEnum.SYSTEM_ERROR;
                    return;
                }
                if (!NativeMethods.IsWeChatRun())
                {
                    sendTask.State = TaskStateEnum.WECHAT_ERROR;
                    return;
                }

                sendTask.DoSendOperation(sysConfig);
                sendTask.State = TaskStateEnum.FINISHED;
            });
        }
    }

    public class SendTask : INotifyPropertyChanged, ICloneable
    {
        static private int _taskCount = 0;
        private string _send2Name = "";
        private string _sendContent = "";
        private TaskStateEnum _taskState;
        private int _sendIndex = 0;

        public TaskCron SendCron { get; set; }
        
        private static Dictionary<TaskStateEnum, string> taskStateMap = new Dictionary<TaskStateEnum, string>
        {
            {TaskStateEnum.WAITED, "待执行" },
            {TaskStateEnum.EXCUTING, "执行中" },
            {TaskStateEnum.FINISHED, "已完成" },
            {TaskStateEnum.WECHAT_ERROR, "执行失败：微信未运行" },
            {TaskStateEnum.SYSTEM_ERROR, "执行失败：系统休眠或者自动锁屏" },
            {TaskStateEnum.INTERNAL_ERROR, "执行失败：内部错误" },
        };

        public SendTask(string send2Name, string sendContent, TaskCron sendCron)
        {
            this._send2Name = send2Name;
            this._sendContent = sendContent;
            this.SendCron = sendCron;
            this._taskState = TaskStateEnum.WAITED;
            this._sendIndex = _taskCount++;
        }

        public int Index {
            get { return _sendIndex; }
        }

        public string Send2Name {
            get { return _send2Name; }
            set {
                if (string.IsNullOrEmpty(value) && value == _send2Name)
                    return;

                _send2Name = value;
                OnPropertyChanged("Send2Name");
            }
        }

        public string SendContent {
            get { return _sendContent; }
            set {
                if (string.IsNullOrEmpty(value) && value == _sendContent)
                    return;

                _sendContent = value;
                OnPropertyChanged("SendContent");
            }
        }

        public string SendCronDescription {
            get {
                return ExpressionDescriptor.GetDescription(this.SendCron.ToString(), new Options()
                {
                    Use24HourTimeFormat = true,
                });
            }
        }

        public TaskStateEnum State {
            get { return _taskState; }
            set {
                if (value == _taskState)
                    return;
                _taskState = value;
                OnPropertyChanged("TaskStateDescription");
            }
        }

        public string TaskStateDescription {
            get { return taskStateMap[_taskState]; }
        }

        public async Task<bool> StartSchedule(IScheduler scheduler, SystemConfig sysConfig)
        {
            try
            {
                // 创建作业
                var jobDetail = JobBuilder.Create<SendTaskJob>()
                                            .WithIdentity("SendTask" + _sendIndex.ToString())
                                            .SetJobData(new JobDataMap() {
                                            new KeyValuePair<string, object>("sendTask", this),
                                            new KeyValuePair<string, object>("sysConfig", sysConfig),
                                            })
                                            .Build();
                // 创建触发器
                var trigger = TriggerBuilder.Create()
                                .WithCronSchedule(this.SendCron.ToString())
                                .Build();
                // 添加调度
                await scheduler.ScheduleJob(jobDetail, trigger);
            } catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Error(ex.Message, "错误");
                return false;
            }
            return true;
        }

        public async Task StopSchedule(IScheduler scheduler)
        {
            JobKey jk = new JobKey("SendTask" + _sendIndex.ToString());
            if (await scheduler.CheckExists(jk))
            {
                await scheduler.DeleteJob(jk);
            }
        }

        public void DoSendOperation(SystemConfig sysConfig)
        {
            // 打开微信应用
            sysConfig.SimulateKeyPress();
            Thread.Sleep(100);

            var simulator = new InputSimulator();
            // 搜索用户名
            simulator.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            simulator.Keyboard.KeyPress(VirtualKeyCode.VK_F);
            simulator.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
            Thread.Sleep(100);

            simulator.Keyboard.TextEntry(_send2Name);
            Thread.Sleep(1000);
            simulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            Thread.Sleep(800);

            // 发送信息
            simulator.Keyboard.TextEntry(_sendContent);
            simulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            Thread.Sleep(1000);

            // 关闭应用
            sysConfig.SimulateKeyPress();
        }

        public object Clone()
        {
            return new SendTask(_send2Name, _sendContent, SendCron);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected internal virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
