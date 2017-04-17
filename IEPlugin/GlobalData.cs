using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Configuration;

namespace Billbank.IEPlugin {
    [ComVisible(false)]
    public class GlobalData {
        private static GlobalData instance = null;

        private bool testMode = false;
        private bool testQuestionAnswer = false;
        
        private GlobalData() {
        }

        public static GlobalData Instance {
            get {
                if (instance == null)
                    instance = new GlobalData();
                return instance;
            }
        }

        internal bool TestMode {
            get {
                return testMode;
            }
            set {
                testMode = value;
            }
        }

        internal bool TestQuestionAnswer {
            get {
                return testQuestionAnswer;
            }
            set {
                testQuestionAnswer = value;
            }
        }
    }
}