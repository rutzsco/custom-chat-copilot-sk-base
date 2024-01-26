// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models;
public static class Filters
{
    public static IEnumerable<string> GetSources()
    {
        return new string[] { "S1", "S2"};
    }

    public static IEnumerable<string> GetModels()
    {
        return new string[] { "Ford Ranger", "TBD" };
    }

    public static IEnumerable<string> GetYears()
    {
        return new string[] { "2019", "TBD" };
    }
}
