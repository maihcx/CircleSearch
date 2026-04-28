using CircleSearch.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CircleSearch.Services
{
    public static class SharedMem
    {
        public static LauncherSettings AppSettings { get; set; }

        public static OverlayLauncherService Launcher { get; set; }
    }
}
