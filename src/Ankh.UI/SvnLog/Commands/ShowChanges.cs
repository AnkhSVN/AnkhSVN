﻿using System;
using System.Collections.Generic;
using System.Text;
using Ankh.Commands;
using SharpSvn;
using Ankh.VS;
using System.IO;
using System.CodeDom.Compiler;
using Microsoft.VisualStudio.Shell.Interop;
using Ankh.Ids;
using Ankh.Scc.UI;
using Ankh.Scc;

namespace Ankh.UI.SvnLog.Commands
{
    [Command(AnkhCommand.LogShowChanges)]
    class ShowChanges : ICommandHandler
    {
        TempFileCollection _collection = new TempFileCollection();
        public void OnUpdate(CommandUpdateEventArgs e)
        {
            ILogControl logWindow = e.Selection.ActiveDialogOrFrameControl as ILogControl;

            if ((logWindow == null) || !logWindow.HasWorkingCopyItems)
            {
                e.Enabled = false;
                return;
            }

            bool one = false;
            foreach (ISvnLogChangedPathItem change in e.Selection.GetSelection<ISvnLogChangedPathItem>())
            {
                if (one)
                {
                    e.Enabled = false;
                    break;
                }
                one = true;
            }
            if (one)
                return;

            // TODO: Remove this code when we're able to handle directories
            SvnItem first = null;
            foreach (SvnItem i in logWindow.WorkingCopyItems)
            {
                first = i;
            }
            if (first == null || first.IsDirectory)
            {
                e.Enabled = false;
                return;
            }

            foreach (Ankh.Scc.ISvnLogItem item in e.Selection.GetSelection<Ankh.Scc.ISvnLogItem>())
            {
                return;
            }

            e.Enabled = false;
        }

        public void OnExecute(CommandEventArgs e)
        {
            long min = long.MaxValue;
            long max = long.MinValue;

            bool touched = false;

            List<string> changedPaths = new List<string>();
            foreach (Ankh.Scc.ISvnLogItem item in e.Selection.GetSelection<Ankh.Scc.ISvnLogItem>())
            {
                min = Math.Min(min, item.Revision);
                max = Math.Max(max, item.Revision);
                touched = true;

                foreach (SvnChangeItem change in item.ChangedPaths)
                {
                    if(!changedPaths.Contains(change.Path))
                        changedPaths.Add(change.Path);
                }
            }
            if (!touched)
            {
                ISvnLogChangedPathItem change = null;
                foreach (ISvnLogChangedPathItem c in e.Selection.GetSelection<ISvnLogChangedPathItem>())
                {
                    change = c;
                    touched = true;
                    break;
                }
                if(change != null)
                {
                    ExecuteChangedPaths(e, change);
                    return;
                }
            }
            if(!touched)
                return;

            if (min == max)
                min--;

            ILogControl logWindow = e.Selection.ActiveDialogOrFrameControl as ILogControl;

            IEnumerable<SvnItem> intersectedItems = LogHelper.IntersectWorkingCopyItemsWithChangedPaths(logWindow.WorkingCopyItems, changedPaths);
            
            // TODO: show dialog when more than one item is returned
            SvnItem workingCopyItem = null;
            foreach (SvnItem item in intersectedItems)
            {
                workingCopyItem = item;
                break;
            }
            if (workingCopyItem == null)
                return;

            SvnRevisionRange range = new SvnRevisionRange(min, max);
            SvnTarget diffTarget = new SvnPathTarget(workingCopyItem.FullPath);
            ExecuteDiff(e, range, diffTarget);
        }

        void ExecuteChangedPaths(CommandEventArgs e, ISvnLogChangedPathItem change)
        {
            IWorkingCopyOperations wcOper = e.GetService<IWorkingCopyOperations>();
            ILogControl logWindow = e.Selection.ActiveDialogOrFrameControl as ILogControl;
            ISvnClientPool clientPool = e.GetService<ISvnClientPool>();

            Uri repositoryRoot = null;
            using(SvnClient client = clientPool.GetClient())
            {
                client.Info(new SvnPathTarget(logWindow.WorkingCopyItems[0].FullPath),
                    delegate(object sender, SvnInfoEventArgs infoEventArgs)
                    {
                        repositoryRoot = infoEventArgs.RepositoryRoot;
                    });
            }
            if (repositoryRoot == null)
                return;

            Uri target = new Uri(repositoryRoot, change.Path.TrimStart('/'));

            ExecuteDiff(e, new SvnRevisionRange(change.Revision - 1, change.Revision), new SvnUriTarget(target, change.Revision));
        }

        void ExecuteDiff(CommandEventArgs e, SvnRevisionRange range, SvnTarget diffTarget)
        {
            IAnkhDiffHandler diff = e.GetService<IAnkhDiffHandler>();
            AnkhDiffArgs da = new AnkhDiffArgs();
            da.BaseFile = diff.GetTempFile(diffTarget, range.StartRevision, false);
            da.BaseTitle = diff.GetTitle(diffTarget, range.StartRevision);
            da.MineFile = diff.GetTempFile(diffTarget, range.EndRevision, false);
            da.MineTitle = diff.GetTitle(diffTarget, range.EndRevision);
            diff.RunDiff(da);
        }
    }

    static class LogHelper
    {
        public static IEnumerable<SvnItem> IntersectWorkingCopyItemsWithChangedPaths(IEnumerable<SvnItem> workingCopyItems, IEnumerable<string> changedPaths)
        {
            foreach (SvnItem i in workingCopyItems)
            {
                foreach (string s in changedPaths)
                {
                    if (i.Status.Uri.ToString().EndsWith(s))
                    {
                        yield return i;
                        break;
                    }
                }
            }
        }
    }
}
