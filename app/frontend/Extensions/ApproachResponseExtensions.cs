// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Extensions;

public static class ApproachResponseExtensions
{
    public static bool HasThoughts(this ApproachResponse response)
    {
        if(response.Context == null)
        {
            return false;
        }

        if(response.Context.Thoughts == null)
        {
            return false;
        }

        return response.Context.Thoughts.Length > 0;
    }

    public static bool HasDataPoints(this ApproachResponse response)
    {
        if (response.Context == null)
        {
            return false;
        }

        if (response.Context.DataPoints == null)
        {
            return false;
        }

        return response.Context.DataPoints.Length > 0;
    }
}
