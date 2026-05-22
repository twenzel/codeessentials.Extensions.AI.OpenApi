# codeessentials.Extensions.AI.OpenApi

[![NuGet](https://img.shields.io/nuget/v/codeessentials.Extensions.AI.OpenApi.svg)](https://nuget.org/packages/codeessentials.Extensions.AI.OpenApi/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build](https://github.com/twenzel/codeessentials.Extensions.AI.OpenApi/actions/workflows/build.yml/badge.svg)](https://github.com/twenzel/codeessentials.Extensions.AI.OpenApi/actions/workflows/build.yml)

[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=twenzel_codeessentials.Extensions.AI.OpenApi&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=twenzel_codeessentials.Extensions.AI.OpenApi)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=twenzel_codeessentials.Extensions.AI.OpenApi&metric=reliability_rating)](https://sonarcloud.io/dashboard?id=twenzel_codeessentials.Extensions.AI.OpenApi)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=twenzel_codeessentials.Extensions.AI.OpenApi&metric=security_rating)](https://sonarcloud.io/dashboard?id=twenzel_codeessentials.Extensions.AI.OpenApi)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=twenzel_codeessentials.Extensions.AI.OpenApi&metric=bugs)](https://sonarcloud.io/dashboard?id=twenzel_codeessentials.Extensions.AI.OpenApi)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=twenzel_codeessentials.Extensions.AI.OpenApi&metric=vulnerabilities)](https://sonarcloud.io/dashboard?id=twenzel_codeessentials.Extensions.AI.OpenApi)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=twenzel_codeessentials.Extensions.AI.OpenApi&metric=coverage)](https://sonarcloud.io/dashboard?id=twenzel_codeessentials.Extensions.AI.OpenApi)

Support for Microsoft.Extensions.AI to create AITools from OpenApi specifications.

## Install

> &gt; dotnet add package codeessentials.Extensions.AI.OpenApi

## Usage

```CSharp
var options = new OpenApiFunctionExecutionParameters
{
    IgnoreNonCompliantErrors = true,
};

var tools = await OpenApiToolFactory.GetToolsFromSpec(new Uri("https://myservice.com/openapi/myservice.json"), options, cancellationToken: cancellationToken);
```
