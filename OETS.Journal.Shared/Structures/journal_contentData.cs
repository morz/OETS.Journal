﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace OETS.Shared.Structures
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Auto)]
    public struct journal_contentData
    {
        public Int32 ID;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 48)]
        public string CodeOborude;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 48)]
        public string NameOborude;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 48)]
        public string Smena;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 48)]
        public string Family;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 48)]
        public string Date;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2048)]
        public string Description;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 48)]
        public string Status;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 48)]
        public string ModifyDate;

        public bool Enable;
    }

    [Serializable]
    [Guid("7764F420-46C6-4b3e-9512-A2F2356FBE45")]
    public class JournalContentData
    {
        public JournalContentData() { }
        public JournalContentData(journal_contentData d)
        {
            this.ID = d.ID;
            this.CodeOborude = d.CodeOborude;
            this.NameOborude = d.NameOborude;
            this.Smena = d.Smena;
            this.Family = d.Family;
            this.Date = d.Date;
            this.Description = d.Description;
            this.Status = d.Status;
            this.Enable = d.Enable;
            this.ModifyDate = d.ModifyDate;

        }
        public int ID { get; set; }
        public string CodeOborude{ get; set; }
        public string NameOborude{ get; set; }
        public string Smena{ get; set; }
        public string Family{ get; set; }
        public string Date{ get; set; }
        public string Description{ get; set; }
        public string Status { get; set; }
        public string ModifyDate{ get; set; }
        public bool Enable{ get; set; }

        public journal_contentData ToStruct()
        {
            var jd = new journal_contentData();
            jd.ID = this.ID;
            jd.CodeOborude = this.CodeOborude;
            jd.NameOborude = this.NameOborude;
            jd.Smena = this.Smena;
            jd.Family = this.Family;
            jd.Date = this.Date;
            jd.Description = this.Description;
            jd.Status = this.Status;
            jd.Enable = this.Enable;
            jd.ModifyDate = this.ModifyDate;
            return jd;
        }
    }
}
