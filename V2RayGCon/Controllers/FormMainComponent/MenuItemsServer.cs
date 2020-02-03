﻿using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using V2RayGCon.Resources.Resx;

namespace V2RayGCon.Controllers.FormMainComponent
{
    class MenuItemsServer : FormMainComponentController
    {
        Services.Cache cache;
        Services.Servers servers;
        Services.ShareLinkMgr slinkMgr;

        public MenuItemsServer(

            // misc
            ToolStripMenuItem refreshSummary,
            ToolStripMenuItem deleteAllServers,
            ToolStripMenuItem deleteSelected,

            // copy
            ToolStripMenuItem copyAsV2cfgLinks,
            ToolStripMenuItem copyAsVmessLinks,
            ToolStripMenuItem copyAsVeeLinks,
            ToolStripMenuItem copyAsVmessSubscriptions,
            ToolStripMenuItem copyAsVeeSubscriptions,

            // batch op
            ToolStripMenuItem speedTestOnSelected,

            ToolStripMenuItem modifySelected,
            ToolStripMenuItem stopSelected,
            ToolStripMenuItem restartSelected,

            // view
            ToolStripMenuItem moveToTop,
            ToolStripMenuItem moveToBottom,
            ToolStripMenuItem foldPanel,
            ToolStripMenuItem expansePanel,
            ToolStripMenuItem sortBySpeed,
            ToolStripMenuItem sortByDate,
            ToolStripMenuItem sortBySummary)
        {
            cache = Services.Cache.Instance;
            servers = Services.Servers.Instance;
            slinkMgr = Services.ShareLinkMgr.Instance;

            InitCtrlSorting(sortBySpeed, sortByDate, sortBySummary);
            InitCtrlView(moveToTop, moveToBottom, foldPanel, expansePanel);

            InitCtrlCopyToClipboard(
                copyAsV2cfgLinks,
                copyAsVmessLinks,
                copyAsVeeLinks,
                copyAsVmessSubscriptions,
                copyAsVeeSubscriptions);

            InitCtrlMisc(
                refreshSummary,
                deleteSelected,
                deleteAllServers);

            InitCtrlBatchOperation(
                stopSelected,
                restartSelected,
                speedTestOnSelected,
                modifySelected);

        }

        #region public method
        public override bool RefreshUI()
        {
            return false;
        }

        public override void Cleanup()
        {
        }
        #endregion

        #region private method
        EventHandler ApplyActionOnSelectedServers(Action action)
        {
            return (s, a) =>
            {
                if (!servers.IsSelecteAnyServer())
                {
                    VgcApis.Misc.Utils.RunInBackground(() => MessageBox.Show(I18N.SelectServerFirst));
                    return;
                }
                action();
            };
        }

        private void InitCtrlBatchOperation(
            ToolStripMenuItem stopSelected,
            ToolStripMenuItem restartSelected,
            ToolStripMenuItem speedTestOnSelected,
            ToolStripMenuItem modifySelected)
        {
            modifySelected.Click += ApplyActionOnSelectedServers(
                () => Views.WinForms.FormBatchModifyServerSetting.GetForm());

            speedTestOnSelected.Click += ApplyActionOnSelectedServers(() =>
            {
                if (!Misc.UI.Confirm(I18N.TestWillTakeALongTime))
                {
                    return;
                }

                servers.RunSpeedTestOnSelectedServersBg();
            });

            stopSelected.Click += ApplyActionOnSelectedServers(() =>
            {
                if (Misc.UI.Confirm(I18N.ConfirmStopAllSelectedServers))
                {
                    servers.StopSelectedServersThen();
                }
            });

            restartSelected.Click += ApplyActionOnSelectedServers(() =>
            {
                if (Misc.UI.Confirm(I18N.ConfirmRestartAllSelectedServers))
                {
                    servers.RestartSelectedServersThen();
                }
            });
        }

        private void InitCtrlMisc(
            ToolStripMenuItem refreshSummary,
            ToolStripMenuItem deleteSelected,
            ToolStripMenuItem deleteAllItems)
        {
            refreshSummary.Click += (s, a) =>
            {
                cache.html.Clear();
                servers.UpdateAllServersSummaryBg();
            };

            deleteAllItems.Click += (s, a) =>
            {
                if (!Misc.UI.Confirm(I18N.ConfirmDeleteAllServers))
                {
                    return;
                }
                Services.Servers.Instance.DeleteAllServersThen();
                Services.Cache.Instance.core.Clear();
            };

            deleteSelected.Click += ApplyActionOnSelectedServers(() =>
            {
                if (!Misc.UI.Confirm(I18N.ConfirmDeleteSelectedServers))
                {
                    return;
                }
                servers.DeleteSelectedServersThen();
            });
        }

        private void InitCtrlCopyToClipboard(
            ToolStripMenuItem copyAsV2cfgLinks,
            ToolStripMenuItem copyAsVmessLinks,
            ToolStripMenuItem copyAsVeeLinks,
            ToolStripMenuItem copyAsVmessSubscriptions,
            ToolStripMenuItem copyAsVeeSubscriptions)
        {
            copyAsVeeSubscriptions.Click += ApplyActionOnSelectedServers(() =>
            {
                var links = EncodeAllServersIntoShareLinks(
                    VgcApis.Models.Datas.Enums.LinkTypes.v);
                var b64Links = Misc.Utils.Base64Encode(links);
                Misc.Utils.CopyToClipboardAndPrompt(b64Links);
            });

            copyAsVmessSubscriptions.Click += ApplyActionOnSelectedServers(() =>
            {
                var links = EncodeAllServersIntoShareLinks(
                    VgcApis.Models.Datas.Enums.LinkTypes.vmess);
                var b64Links = Misc.Utils.Base64Encode(links);
                Misc.Utils.CopyToClipboardAndPrompt(b64Links);
            });

            copyAsV2cfgLinks.Click += ApplyActionOnSelectedServers(() =>
            {
                var links = EncodeAllServersIntoShareLinks(
                    VgcApis.Models.Datas.Enums.LinkTypes.v2cfg);

                Misc.Utils.CopyToClipboardAndPrompt(links);
            });

            copyAsVmessLinks.Click += ApplyActionOnSelectedServers(() =>
            {
                var links = EncodeAllServersIntoShareLinks(
                           VgcApis.Models.Datas.Enums.LinkTypes.vmess);
                Misc.Utils.CopyToClipboardAndPrompt(links);
            });

            copyAsVeeLinks.Click += ApplyActionOnSelectedServers(() =>
            {
                var links = EncodeAllServersIntoShareLinks(VgcApis.Models.Datas.Enums.LinkTypes.v);
                Misc.Utils.CopyToClipboardAndPrompt(links);
            });
        }

        private void InitCtrlView(
            ToolStripMenuItem moveToTop,
            ToolStripMenuItem moveToBottom,
            ToolStripMenuItem collapsePanel,
            ToolStripMenuItem expansePanel)
        {
            expansePanel.Click += ApplyActionOnSelectedServers(() =>
            {
                SetServerItemPanelCollapseLevel(0);
            });

            collapsePanel.Click += ApplyActionOnSelectedServers(() =>
            {
                SetServerItemPanelCollapseLevel(1);
            });

            moveToTop.Click += ApplyActionOnSelectedServers(() =>
            {
                SetServerItemsIndex(0);
            });

            moveToBottom.Click += ApplyActionOnSelectedServers(() =>
            {
                SetServerItemsIndex(double.MaxValue);
            });
        }

        private void InitCtrlSorting(
            ToolStripMenuItem sortBySpeed,
            ToolStripMenuItem sortByDate,
            ToolStripMenuItem sortBySummary)
        {
            sortByDate.Click += ApplyActionOnSelectedServers(
                () => servers.SortSelectedByLastModifiedDate());

            sortBySummary.Click += ApplyActionOnSelectedServers(
                () => servers.SortSelectedBySummary());

            sortBySpeed.Click += ApplyActionOnSelectedServers(
                () => servers.SortSelectedBySpeedTest());
        }

        void SetServerItemPanelCollapseLevel(int collapseLevel)
        {
            collapseLevel = Misc.Utils.Clamp(collapseLevel, 0, 3);
            servers
                .GetAllServersOrderByIndex()
                .Where(s => s.GetCoreStates().IsSelected())
                .Select(s =>
                {
                    s.GetCoreStates().SetFoldingState(collapseLevel);
                    return true;
                })
                .ToList(); // force linq to execute
        }

        void RemoveAllControlsAndRefreshFlyPanel()
        {
            var panel = GetFlyPanel();
            panel.RemoveAllServersConrol();
            panel.RefreshUI();
        }

        void SetServerItemsIndex(double index)
        {
            servers.GetAllServersOrderByIndex()
                .Where(s => s.GetCoreStates().IsSelected())
                .Select(s =>
                {
                    s.GetCoreStates().SetIndex(index);
                    return true;
                })
                .ToList(); // force linq to execute

            RemoveAllControlsAndRefreshFlyPanel();
        }

        string EncodeAllServersIntoShareLinks(
            VgcApis.Models.Datas.Enums.LinkTypes linkType)
        {
            var serverList = servers.GetAllServersOrderByIndex();

            StringBuilder result = new StringBuilder("");

            foreach (var server in serverList)
            {
                if (!server.GetCoreStates().IsSelected())
                {
                    continue;
                }

                var configString = server.GetConfiger().GetConfig();
                var shareLink = slinkMgr.EncodeConfigToShareLink(
                    configString, linkType);

                if (!string.IsNullOrEmpty(shareLink))
                {
                    result
                        .Append(shareLink)
                        .Append(Environment.NewLine);
                }
            }

            return result.ToString();
        }

        FlyServer GetFlyPanel()
        {
            return this.GetContainer()
                .GetComponent<FlyServer>();
        }
        #endregion
    }
}