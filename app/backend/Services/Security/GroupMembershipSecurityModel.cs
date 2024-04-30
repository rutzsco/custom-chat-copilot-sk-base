// Copyright (c) Microsoft. All rights reserved.
using System.Security.Claims;
using MinimalApi.Services.Profile;
namespace MinimalApi.Services.Security;

public static class GroupMembershipSecurityModel
{
    public static bool HasAccess(this UserInformation userInformation, ProfileDefinition profileDefinition)
    {
        return userInformation.Groups.Any(ug => profileDefinition.SecurityModelGroupMembership.Contains(ug));
    }
}
