﻿using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace CodeOwls.BIPS
{
    public class PathNodeProcessor : PathNodeProcessorBase
    {
        private readonly BipsDrive _drive;

        public PathNodeProcessor(BipsDrive drive)
        {
            _drive = drive;
        }

        private INodeFactory _root;

        public override System.Collections.Generic.IEnumerable<INodeFactory> ResolvePath(
            PowerShell.Provider.PathNodeProcessors.IContext context, string path)
        {
            var driveInfo = GetDriveFromPath(context, path);

            if (null == driveInfo)
            {
                context.WriteWarning(String.Format("Could not determine drive for path [{0}]", path));
                return base.ResolvePath(context, path);
            }

            string fileOrServerPath = Regex.Replace(path, @"^[^::]+::", String.Empty);

            var re = new Regex("^.*(" + Regex.Escape(driveInfo.Root) + ")(.*)$", RegexOptions.IgnoreCase);
            var matches = re.Match(path);
            fileOrServerPath = matches.Groups[1].Value;
            path = matches.Groups[2].Value;

            if (File.Exists(fileOrServerPath) || Directory.Exists(fileOrServerPath))
            {
                _root = new BipsFileRootNodeFactory(_drive, fileOrServerPath);
            }
            else
            {
                _root = new BipsRootNodeFactory(_drive);
            }

            return base.ResolvePath(context, path);
        }

        private static PSDriveInfo GetDriveFromPath(IContext context, string path)
        {
            if (null != context.Drive && !String.IsNullOrEmpty(context.Drive.Root))
            {
                return context.Drive;
            }
            return (from drive in context.SessionState.Provider.GetOne("BIPS").Drives
                let root = drive.Root
                let length = drive.Root.Length
                let rex = new Regex("^.*(" + Regex.Escape(root) + ")(.*)$", RegexOptions.IgnoreCase)
                where rex.IsMatch(path)
                orderby length descending
                select drive).FirstOrDefault();
        }

        protected override INodeFactory Root
        {
            get
            {
                return _root;
            }
        }
    }
}