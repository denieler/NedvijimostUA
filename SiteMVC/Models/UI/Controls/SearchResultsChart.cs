﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SiteMVC.Models.UI.Controls
{
    public class SearchResult
    {
        public int fullCount { get; set; }
        public int withoutSubPurchaseCount { get; set; }

        public DateTime date { get; set; }
    }
}