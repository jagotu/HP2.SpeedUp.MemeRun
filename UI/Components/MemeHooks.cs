using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LiveSplit.ComponentUtil;

namespace LiveSplit.UI.Components
{
    abstract class MemeGame
    {
        public DeepPointer speedptr;
        public string Name;
        public Process process;
        public IntPtr dllBase;


        public abstract string getDllName();

        public abstract string getExeName();

        public virtual void bind(Process process)
        {
            this.process = process;
            this.dllBase = process.ModulesWow64Safe().FirstOrDefault(x => x.ModuleName.ToLower() == getDllName()).BaseAddress;
        }

        public bool tryFind()
        {
            Process p = Process.GetProcesses().FirstOrDefault(x => x.ProcessName.ToLower().Equals(getExeName()));

            if (p == null || p.HasExited)
                return false;

            // process is up, check if dll is loaded
            ProcessModuleWow64Safe dll = p.ModulesWow64Safe().FirstOrDefault(x => x.ModuleName.ToLower() == getDllName());
            if (dll == null)
                return false;

            bind(p);

            return true;
        }
    }

    class HP1Game : MemeGame
    {

        public override string getDllName()
        {
            return "render.dll";
        }

        public override string getExeName()
        {
            return "hp";
        }

        public override void bind(Process process)
        {
            base.bind(process);
            this.speedptr = new DeepPointer(this.dllBase + 0x0004B360, new int[] { 0x7C, 0x3B0 });
            this.process = process;
            this.Name = "HP1";
        }
    }

    class HP2Game : MemeGame
    {

        public override string getDllName()
        {
            return "render.dll";
        }

        public override string getExeName()
        {
            return "game";
        }

        public override void bind(Process process)
        {
            base.bind(process);
            this.speedptr = new DeepPointer(this.dllBase + 0x0004DA60, new int[] { 0xB4, 0x404 });
            this.process = process;
            this.Name = "HP2";
        }
    }

    class HP3Game : MemeGame
    {

        public override string getDllName()
        {
            return "engine.dll";
        }

        public override string getExeName()
        {
            return "hppoa";
        }

        public override void bind(Process process)
        {
            base.bind(process);
            this.speedptr = new DeepPointer(dllBase + 0x004E0298, new int[] { 0x68, 0x9C, 0x484 });
            this.process = process;
            this.Name = "HP3";
        }
    }

    class MemeHooks
    {
        public string state;

        [DllImport("winmm.dll")]
        static extern uint timeBeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dll")]
        static extern uint timeEndPeriod(uint uMilliseconds);

        private Task _thread;
        private CancellationTokenSource _cancelSource;

        static MemeGame[] games = new MemeGame[] { new HP1Game(), new HP2Game(), new HP3Game() };

        public float targetSpeed = 1.0f;
        public float lastSpeed = 1.0f;

        public MemeHooks()
        {
            this.state = "Loading...";
            _cancelSource = new CancellationTokenSource();
            _thread = Task.Factory.StartNew(() => MemoryReadThread(_cancelSource));
        }

        MemeGame GetGameProcess()
        {
            foreach(MemeGame game in games)
            {
                if (game.tryFind())
                    return game;
            }
            return null;
        }


        void HandleGame(MemeGame game, CancellationTokenSource cts)
        {

     

            while (!game.process.HasExited && !cts.IsCancellationRequested)
            {
                // iteration must never take longer than 1 tick
                byte[] CurrentSpeedBytes = game.speedptr.DerefBytes(game.process, 4);
                float CurrentSpeed = BitConverter.ToSingle(CurrentSpeedBytes, 0);
                this.state = "Current game speed = " + CurrentSpeed.ToString("0.00") + " (" + game.Name + ")";

                IntPtr ptr;
                

                if((CurrentSpeed == 1.0f || (CurrentSpeed == lastSpeed && lastSpeed != targetSpeed)) && game.speedptr.DerefOffsets(game.process, out ptr))
                {
                    byte[] buffer = BitConverter.GetBytes(targetSpeed);
                    game.process.WriteBytes(ptr, buffer);
                    lastSpeed = targetSpeed;
                }

            }
        }

        void MemoryReadThread(CancellationTokenSource cts)
        {
            // force windows timer resolution to 1ms. it probably already is though, from the game.
            timeBeginPeriod(1);
            // we do a lot of timing critical stuff so this may help out
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
            while (true)
            {
                state = "Waiting for process";
                try
                {
                    MemeGame game;
                    game = this.GetGameProcess();
                    while (game == null)
                    {
                        Thread.Sleep(750);

                        if (cts.IsCancellationRequested)
                            goto ret;

                        game = this.GetGameProcess();
                    }

                    this.HandleGame(game, cts);

                    if (cts.IsCancellationRequested)
                        goto ret;
                }
                catch (Exception ex) // probably a Win32Exception on access denied to a process
                {
                    Trace.WriteLine(ex.ToString());
                    Thread.Sleep(1000);
                }
            }

        ret:

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            timeEndPeriod(1);
        }
    }
}

