using System;
using System.Collections.Generic;

namespace trainingapp.Models;

public partial class SummaryOfSalesByYear
{
    public DateTime? Shippeddate { get; set; }

    public int? Orderid { get; set; }

    public decimal? Subtotal { get; set; }
}
