using System;
using System.Collections.Generic;

namespace trainingapp.Models;

public partial class SalesTotalsByAmount
{
    public decimal? Saleamount { get; set; }

    public int? Orderid { get; set; }

    public string? Companyname { get; set; }

    public DateTime? Shippeddate { get; set; }
}
