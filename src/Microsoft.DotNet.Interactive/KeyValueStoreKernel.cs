﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    public class KeyValueStoreKernel :
        Kernel,
        ISupportGetValue,
        ISupportSetValue,
        IKernelCommandHandler<SubmitCode>
    {
        internal const string DefaultKernelName = "value";

        private readonly ConcurrentDictionary<string, object> _values = new();

        public KeyValueStoreKernel() : base(DefaultKernelName)
        {
        }

        public Task SetValueAsync(string name, object value, Type declaredType = null)
        {
            _values[name] = value;
            return Task.CompletedTask;
        }

        public IReadOnlyCollection<KernelValueInfo> GetValueInfos() =>
            _values.Select(e => new KernelValueInfo(e.Key, typeof(string))).ToArray();

        public bool TryGetValue<T>(string name, out T value)
        {
            if (_values.TryGetValue(name, out var obj) &&
                obj is T t)
            {
                value = t;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            var parseResult = command.KernelNameDirectiveNode.GetDirectiveParseResult();

            var value = command.LanguageNode.Text.Trim();

            var options = ValueDirectiveOptions.Create(parseResult);

            if (options.FromFile is {})
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    context.Fail(command, message: "The --from-file option cannot be used in combination with a content submission.");
                }

                return;
            }

            if (options.FromUrl is {})
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    context.Fail(command, message: "The --from-url option cannot be used in combination with a content submission.");
                }

                return;
            }

            await StoreValueAsync(value, options, context);
        }

        private async Task StoreValueAsync(
            string value,
            ValueDirectiveOptions options,
            KernelInvocationContext context)
        {
            await SetValueAsync(options.Name, value);

            if (options.MimeType is { } mimeType)
            {
                context.DisplayAs(value, mimeType);
            }
        }

        protected override ChooseKernelDirective CreateChooseKernelDirective()
        {
            var nameOption = new Option<string>(
                "--name",
                "The name of the value to create. You can use #!share to retrieve this value from another subkernel.")
            {
                IsRequired = true
            };

            var fromUrlOption = new Option<Uri>(
                "--from-url",
                description: "Specifies a URL whose content will be stored.");

            var fromFileOption = new Option<FileInfo>(
                "--from-file",
                description: "Specifies a file whose contents will be stored.",
                parseArgument: result =>
                {
                    var filePath = result.Tokens.Single().Value;

                    var fromUrlResult = result.FindResultFor(fromUrlOption);

                    if (fromUrlResult is {})
                    {
                        result.ErrorMessage = $"The {fromUrlResult.Token.Value} and {((OptionResult) result.Parent).Token.Value} options cannot be used together.";
                        return null;
                    }
                    else if (!File.Exists(filePath))
                    {
                        result.ErrorMessage = Resources.Instance.FileDoesNotExist(filePath);
                        return null;
                    }
                    else
                    {
                        return new FileInfo(filePath);
                    }
                });

            var mimeTypeOption = new Option<string>(
                    "--mime-type",
                    "A mime type for the value. If specified, displays the value immediately as an output using the specified mime type.")
                .AddSuggestions((_,__) => new[]
                {
                    "application/json",
                    "text/html",
                    "text/plain",
                    "text/csv"
                });

            return new ValueDirective(this, "Stores a value that can then be shared with other subkernels.")
            {
                nameOption,
                fromFileOption,
                fromUrlOption,
                mimeTypeOption
            };
        }

        private class ValueDirective : ChooseKernelDirective
        {
            private readonly KeyValueStoreKernel _kernel;

            public ValueDirective(KeyValueStoreKernel kernel, string description = null) : base(kernel, description)
            {
                _kernel = kernel;
            }

            protected override async Task Handle(KernelInvocationContext kernelInvocationContext, InvocationContext commandLineInvocationContext)
            {
                var options = ValueDirectiveOptions.Create(commandLineInvocationContext.ParseResult);

                if (options.FromFile is {} fromFile)
                {
                    var value = File.ReadAllText(fromFile.FullName);
                    await _kernel.StoreValueAsync(value, options, kernelInvocationContext);
                }
                else if (options.FromUrl is {} fromUrl)
                {
                    var client = new HttpClient();
                    var response = await client.GetAsync(fromUrl, kernelInvocationContext.CancellationToken);
                    var value = await response.Content.ReadAsStringAsync();
                    await _kernel.StoreValueAsync(value, options, kernelInvocationContext);
                }

                await base.Handle(kernelInvocationContext, commandLineInvocationContext);
            }
        }

        private class ValueDirectiveOptions
        {
            private static readonly ModelBinder<ValueDirectiveOptions> _modelBinder = new();

            public static ValueDirectiveOptions Create(ParseResult parseResult) => _modelBinder.CreateInstance(new BindingContext(parseResult)) as ValueDirectiveOptions;

            public string Name { get; set; }

            public FileInfo FromFile { get; set; }

            public Uri FromUrl { get; set; }

            public string MimeType { get; set; }
        }
    }
}