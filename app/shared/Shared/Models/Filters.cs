// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models;

public record UserSelectionModel(IEnumerable<UserSelectionOption> Options);

public class UserSelectionOption
{
    public string Name { get; set; }
    public IEnumerable<string> Values { get; set; }
    public string? SelectedValue { get; set; }

    public UserSelectionOption(string name, IEnumerable<string> values, string? selectedValue = null)
    {
        Name = name;
        Values = values;
        SelectedValue = selectedValue;
    }
}
