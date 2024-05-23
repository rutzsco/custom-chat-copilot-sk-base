// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.OpenAI;
namespace MinimalApi.Extensions;

public class OpenAIClientFacade
{
    public OpenAIClientFacade(string kernel3DeploymentName, Kernel kernel3, string kernel4DeploymentName, Kernel kernel4)
    {
        Kernel3 = kernel3;
        Kernel4 = kernel4;

        Kernel3DeploymentName = kernel3DeploymentName;
        Kernel4DeploymentName = kernel4DeploymentName;
    }

    public string Kernel3DeploymentName { get; set; }
    public Kernel Kernel3 { get; set; }

    public string Kernel4DeploymentName { get; set; }
    public Kernel Kernel4 { get; set; }


    public Kernel GetKernel(bool chatGPT4)
    {
        if (chatGPT4)
        {
            return Kernel4;
        }

        return Kernel3;
    }
    public string GetKernelDeploymentName(bool chatGPT4)
    {
        if (chatGPT4)
        {
            return Kernel4DeploymentName;
        }

        return Kernel3DeploymentName;
    }
}
