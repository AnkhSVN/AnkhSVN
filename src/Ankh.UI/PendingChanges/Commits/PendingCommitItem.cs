// $Id$
//
// Copyright 2008 The AnkhSVN Project
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Ankh.Scc;
using Ankh.VS;
using Ankh.UI.VSSelectionControls;

namespace Ankh.UI.PendingChanges.Commits
{
    class PendingCommitItem : SmartListViewItem
    {
        readonly PendingChange _change;
        string _lastChangeList;

        public PendingCommitItem(PendingCommitsView view, PendingChange change)
            : base(view)
        {
            if (change == null)
                throw new ArgumentNullException("change");

            _change = change;        

            //initially check only if this change does not belong to an "ignore" change list
            Checked = !change.IgnoreOnCommit;

            RefreshText(view.Context);
        }

        public void RefreshText(IAnkhServiceProvider context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            IFileStatusCache cache = context.GetService<IFileStatusCache>();

            ImageIndex = PendingChange.IconIndex;
            SvnItem item = cache[FullPath];

            if (item == null)
                throw new InvalidOperationException(); // Item no longer valued

            PendingChangeStatus pcs = PendingChange.Change ?? new PendingChangeStatus(PendingChangeKind.None);

            SetValues(
                pcs.PendingCommitText,
                _lastChangeList = PendingChange.ChangeList,
                GetDirectory(item),
                PendingChange.FullPath,
                item.IsLocked ? PCStrings.LockedValue : "", // Locked
                SafeDate(item.Modified), // Modified
                PendingChange.Name,
                PendingChange.RelativePath,
                PendingChange.Project,
                item.Extension,
                SafeWorkingCopy(item));

            if (!SystemInformation.HighContrast)
            {
                System.Drawing.Color clr = System.Drawing.Color.Black;

                if (item.IsConflicted || PendingChange.Kind == PendingChangeKind.WrongCasing)
                    clr = System.Drawing.Color.Red;
                else if (item.IsDeleteScheduled)
                    clr = System.Drawing.Color.DarkRed;
                else if (item.Status.IsCopied || item.Status.CombinedStatus == SharpSvn.SvnStatus.Added || !item.IsVersioned)
                    clr = System.Drawing.Color.FromArgb(100, 0, 100);
                else if (item.IsModified)
                    clr = System.Drawing.Color.DarkBlue;

                ForeColor = clr;
            }
        }

        private string SafeDate(DateTime dateTime)
        {
            if (dateTime.Ticks == 0 || dateTime.Ticks == 1)
                return "";

            DateTime n = dateTime.ToLocalTime();

            if (n < DateTime.Now - new TimeSpan(24, 0, 0))
                return n.ToString("d");
            else
                return n.ToString("T");
        }

        private string GetDirectory(SvnItem svnItem)
        {
            if (svnItem.IsDirectory)
                return svnItem.FullPath;
            else
                return svnItem.Directory;
        }

        static string SafeWorkingCopy(SvnItem svnItem)
        {
            SvnWorkingCopy wc = svnItem.WorkingCopy;
            if (wc == null)
                return "";

            return wc.FullPath;
        }

        /// <summary>
        /// Gets the full path.
        /// </summary>
        /// <value>The full path.</value>
        public PendingChange PendingChange
        {
            get { return _change; }
        }

        /// <summary>
        /// Gets the full path.
        /// </summary>
        /// <value>The full path.</value>
        public string FullPath
        {
            get { return _change.FullPath; }
        }


        /// <summary>
        /// Gets change list at the time of the last refresh
        /// </summary>
        /// <value>The last change list.</value>
        /// <remarks>Used for checking for changelist changes</remarks>
        internal string LastChangeList
        {
            get { return _lastChangeList; }
        }
    }
}
