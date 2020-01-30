﻿using System;
using System.Threading;
using V2RayGCon.Resources.Resx;

namespace V2RayGCon.Controllers.CoreServerComponent
{
    sealed public class CoreCtrl :
        VgcApis.BaseClasses.ComponentOf<CoreServerCtrl>,
        VgcApis.Interfaces.CoreCtrlComponents.ICoreCtrl
    {
        Libs.V2Ray.Core v2rayCore;
        Services.Settings setting;
        Services.ConfigMgr configMgr;

        public CoreCtrl(
            Services.Settings setting,
            Services.ConfigMgr configMgr)
        {
            this.setting = setting;
            this.configMgr = configMgr;
        }

        CoreStates coreStates;
        Configer configer;
        Logger logger;

        public override void Prepare()
        {
            v2rayCore = new Libs.V2Ray.Core(setting);

            coreStates = GetSibling<CoreStates>();
            configer = GetSibling<Configer>();
            logger = GetSibling<Logger>();
        }

        #region public mehtods
        // 非正常终止时调用 
        public void SetTitle(string title) => v2rayCore.title = title;

        public void BindEvents()
        {
            v2rayCore.OnLog += OnLogHandler;
            v2rayCore.OnCoreStatusChanged += OnCoreStateChangedHandler;
        }
        public void ReleaseEvents()
        {
            v2rayCore.OnLog -= OnLogHandler;
            v2rayCore.OnCoreStatusChanged -= OnCoreStateChangedHandler;
        }

        public VgcApis.Models.Datas.StatsSample TakeStatisticsSample()
        {
            var statsPort = coreStates.GetStatPort();
            if (!setting.isEnableStatistics
                || statsPort <= 0)
            {
                return null;
            }

            var up = this.v2rayCore.QueryStatsApi(statsPort, true);
            var down = this.v2rayCore.QueryStatsApi(statsPort, false);
            return new VgcApis.Models.Datas.StatsSample(up, down);
        }

        public void RestartCore()
        {
            AutoResetEvent done = new AutoResetEvent(false);
            RestartCoreThen(() => done.Set());
            done.WaitOne();
        }

        public void StopCoreQuiet() => v2rayCore.StopCoreThen(null);

        public void StopCore()
        {
            AutoResetEvent done = new AutoResetEvent(false);
            StopCoreThen(() => done.Set());
            done.WaitOne();
        }

        public void StopCoreThen() =>
            VgcApis.Misc.Utils.RunInBackground(() => StopCoreWorker(null));

        public void StopCoreThen(Action next) =>
            VgcApis.Misc.Utils.RunInBackground(() => StopCoreWorker(next));

        public void RestartCoreThen() => RestartCoreThen(null);
        public void RestartCoreThen(Action next) =>
            VgcApis.Misc.Utils.RunInBackground(() => RestartCoreWorker(next));

        public bool IsCoreRunning() => v2rayCore.isRunning;

        public void RunSpeedTest() =>
            SpeedTestWorker(configer.GetConfig());
        #endregion

        #region private methods
        void OnCoreStateChangedHandler(object sender, EventArgs args)
        {
            if (v2rayCore.isRunning)
            {
                GetParent().InvokeEventOnCoreStart();
            }
            else
            {
                coreStates.SetStatPort(0);
                GetParent().InvokeEventOnCoreStop();
            }
        }

        string TranslateSpeedTestResult(long speedtestDelay)
        {
            if (speedtestDelay == long.MaxValue)
            {
                return I18N.Timeout;
            }
            return $"{speedtestDelay}ms";
        }

        void SpeedTestWorker(string rawConfig)
        {
            long avgDelay = -1;
            long curDelay = long.MaxValue;
            var cycles = Math.Max(1, setting.isUseCustomSpeedtestSettings ? setting.CustomSpeedtestCycles : 1);

            coreStates.SetStatus(I18N.Testing);
            logger.Log(I18N.Testing);
            for (int i = 0; i < cycles; i++)
            {
                curDelay = configMgr.RunDefaultSpeedTest(rawConfig, coreStates.GetTitle(), (s, a) => logger.Log(a.Data));
                logger.Log(I18N.CurSpeedtestResult + TranslateSpeedTestResult(curDelay));
                if (curDelay == long.MaxValue)
                {
                    continue;
                }
                avgDelay = VgcApis.Misc.Utils.SpeedtestMean(avgDelay, curDelay, VgcApis.Models.Consts.Config.CustomSpeedtestMeanWeight);
            }

            // all speedtest timeout 
            if (avgDelay <= 0)
            {
                avgDelay = long.MaxValue;
            }
            var speedtestResult = TranslateSpeedTestResult(avgDelay);
            coreStates.SetStatus(speedtestResult);
            coreStates.SetSpeedTestResult(avgDelay);
            logger.Log(I18N.AvgSpeedtestResult + speedtestResult);
        }

        void OnLogHandler(object sender, VgcApis.Models.Datas.StrEvent arg) =>
            logger.Log(arg.Data);

        void StopCoreWorker(Action next)
        {
            GetParent().InvokeEventOnCoreClosing();
            v2rayCore.StopCoreThen(
                () =>
                {
                    // Libs.V2Ray.Core will fire OnCoreStop
                    // container.InvokeEventOnCoreStop();
                    next?.Invoke();
                });
        }

        void RestartCoreWorker(Action next)
        {
            var finalConfig = configer.GetFinalConfig();
            if (finalConfig == null)
            {
                StopCoreThen(next);
                return;
            }

            v2rayCore.title = coreStates.GetTitle();
            v2rayCore.RestartCoreThen(
                finalConfig.ToString(),
                () =>
                {
                    // Libs.V2Ray.Core will fire OnCoreStart
                    // container.InvokeEventOnCoreStart();
                    next?.Invoke();
                },
                Misc.Utils.GetEnvVarsFromConfig(finalConfig));
        }
        #endregion
    }
}