using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsInput.Native;

namespace WeChatHelper
{
    public class SystemConfig : INotifyPropertyChanged
    {
        public Key Key { get; private set; }

        public ModifierKeys Modifiers { get; private set; }

        private string _passwd;

        public SystemConfig(Key key, ModifierKeys modifiers, string passwd)
        {
            Key = key;
            Modifiers = modifiers;
            _passwd = passwd;
        }

        public string PasswdString {
            get {
                return _passwd;
            }
            set {
                if (value == _passwd) return;
                _passwd = value;
                OnPropertyChanged("PasswdString");
            }
        }

        public string KeyString {
            get {
                var str = new StringBuilder();

                if (Modifiers.HasFlag(ModifierKeys.Control))
                    str.Append("Ctrl + ");
                if (Modifiers.HasFlag(ModifierKeys.Shift))
                    str.Append("Shift + ");
                if (Modifiers.HasFlag(ModifierKeys.Alt))
                    str.Append("Alt + ");
                if (Modifiers.HasFlag(ModifierKeys.Windows))
                    str.Append("Win + ");

                str.Append(Key);

                return str.ToString();
            }
        }

        public void UpdateHotKey(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
            OnPropertyChanged("KeyString");
        }

        public void SimulateKeyPress()
        {
            var simulator = new WindowsInput.InputSimulator();
            List<VirtualKeyCode> modKeyList = new List<VirtualKeyCode>();
            if (Modifiers.HasFlag(ModifierKeys.Control))
                modKeyList.Add(VirtualKeyCode.CONTROL);
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                modKeyList.Add(VirtualKeyCode.SHIFT);
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                modKeyList.Add(VirtualKeyCode.MENU);
            if (Modifiers.HasFlag(ModifierKeys.Windows))
                modKeyList.Add(VirtualKeyCode.LWIN);

            foreach (VirtualKeyCode key in modKeyList)
            {
                simulator.Keyboard.KeyDown(key);
            }

            simulator.Keyboard.KeyPress((VirtualKeyCode)KeyInterop.VirtualKeyFromKey(Key));

            foreach (VirtualKeyCode key in modKeyList)
            {
                simulator.Keyboard.KeyUp(key);
            }
        }

        // TODO: Don't work
        public void UnlockScreenByPasswd()
        {
            var simulator = new WindowsInput.InputSimulator();
            if (!NativeMethods.IsWorkstationLocked())
            {
                return;
            }
            simulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            Thread.Sleep(2000);

            simulator.Keyboard.TextEntry(_passwd);
            simulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected internal virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
