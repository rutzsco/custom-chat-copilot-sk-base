// Copyright (c) Microsoft. All rights reserved.
using System.Security.Claims;
using MinimalApi.Services.Profile;
using Shared.Models;
namespace MinimalApi.Services.Security;

public static class GroupMembershipSecurityModel
{
    public static bool HasAccess(this UserInformation userInformation, ProfileDefinition profileDefinition)
    {
        if(profileDefinition.SecurityModel == "Group")
        {
            return userInformation.Groups.Any(ug => profileDefinition.SecurityModelGroupMembership.Contains(ug));
        }
        return true;
    }

    public static IEnumerable<ProfileDefinition> GetAuthorizedProfiles(this List<ProfileDefinition> profiles, List<string> userGroups)
    {
        foreach (var profile in profiles)
        {
            if (profile.SecurityModel == "Group")
            {
                if (userGroups.Any(g => profile.SecurityModelGroupMembership.Contains(g)))
                {
                    yield return profile;
                }
            }
            else
                yield return profile;
        }
    }
}
