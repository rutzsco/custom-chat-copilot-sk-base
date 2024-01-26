// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.OpenAI;
namespace MinimalApi.Extensions;

public class OpenAIClientFacade
{
    public OpenAIClientFacade(Kernel kernel3, Kernel kernel4, OpenAIClient openAIClient)
    {
        Kernel3 = kernel3;
        Kernel4 = kernel4;
        OpenAIClient = openAIClient;
    }

    public Kernel Kernel3 { get; set; }
    public Kernel Kernel4 { get; set; }
    public OpenAIClient OpenAIClient { get; set; }

    public Kernel GetKernel(bool chatGPT4)
    {
        if (chatGPT4)
        {
            return Kernel4;
        }

        return Kernel3;
    }

    public Kernel GetChatGPT(bool chatGPT4)
    {
        if (chatGPT4)
        {
            return Kernel4;
        }

        return Kernel3;
    }

}
