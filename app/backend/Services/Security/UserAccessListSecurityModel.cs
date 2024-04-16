// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Services.Security;

public class UserAccessListSecurityModel
{
    public bool HasAccess(string userPrincipalId)
    {
        return true;
    }
}
