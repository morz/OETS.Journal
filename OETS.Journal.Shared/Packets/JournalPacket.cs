using OETS.Shared;
using OETS.Shared.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OETS.Shared
{
    public partial class JournalPacket: BasePacket
    {
        public journal_contentData Data { get; set; }

        public JournalPacket()
        {
        }

        public JournalPacket(journal_contentData data)
            : base("#" + data.ID)
        {
            Data = data;
        }
    }
}
