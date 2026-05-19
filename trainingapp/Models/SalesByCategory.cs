using System;
using System.Collections.Generic;

namespace trainingapp.Models;

public partial class SalesByCategory
{
    public int? Categoryid { get; set; }

    public string? Categoryname { get; set; }

    public string? Productname { get; set; }

    public decimal? Productsales { get; set; }
}
