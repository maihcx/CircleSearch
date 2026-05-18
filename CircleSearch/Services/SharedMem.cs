using CircleSearch.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CircleSearch.Services
{
    public static class SharedMem
    {
        public static LauncherSettings? AppSettings { get; set; }

        public static OverlayLauncherService? Launcher { get; set; }

        private static bool _isScrollToUpdateCard = false;
        public static bool IsScrollToUpdateCard
        {
            get
            {
                bool cacheVal = _isScrollToUpdateCard;
                _isScrollToUpdateCard = false;
                return cacheVal;
            }
            set
            {
                _isScrollToUpdateCard = value;
            }
        }
    }
}
