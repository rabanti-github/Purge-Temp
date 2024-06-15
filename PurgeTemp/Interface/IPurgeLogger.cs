using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeTemp.Interface
{
	public  interface IPurgeLogger
	{
		public void PurgeInfo(string source, string file);

		public void MoveInfo(string source, string target, string file);

		public void SkipMoveInfo(string source, string target, int skippedFiles);

		public void SkipPurgeInfo(string source, int skippedFiles);

		public void SkipInfo(string source, string destination, bool isPurged, int skippedFiles);

		public void Info(string source, string destination, bool isPurged, string file);
	}
}
