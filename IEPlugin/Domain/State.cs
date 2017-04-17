using System;
using System.Collections.Generic;
using System.Text;

namespace Billbank.IEPlugin.Domain {
    public enum State {
        IDLE, IN_PROGRESS, IN_PROGRESS_CONFIRM, PAID, INVALID // invalid also means Cancelled
    }
}
