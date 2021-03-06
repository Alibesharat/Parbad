// Copyright (c) Parbad. All rights reserved.
// Licensed under the GNU GENERAL PUBLIC License, Version 3.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Parbad.Abstraction;
using Parbad.Exceptions;
using Parbad.TrackingNumberProviders;

namespace Parbad.InvoiceBuilder
{
    /// <summary>
    /// A builder which helps to build an invoice.
    /// </summary>
    public interface IInvoiceBuilder
    {
        IServiceProvider Services { get; }

        /// <summary>
        /// Sets an <see cref="ITrackingNumberProvider"/> which generates a new tracking numbers for each payment requests.
        /// </summary>
        /// <param name="provider">An implementation of <see cref="ITrackingNumberProvider"/>.</param>
        /// <exception cref="ArgumentNullException"></exception>
        IInvoiceBuilder SetTrackingNumberProvider(ITrackingNumberProvider provider);

        /// <summary>
        /// Sets the amount of the invoice.
        /// </summary>
        /// <param name="amount">The amount of invoice.</param>
        IInvoiceBuilder SetAmount(Money amount);

        /// <summary>
        /// Sets the callback URL. It will be used by the gateway for redirecting the client
        /// again to your website.
        /// </summary>
        /// <param name="url">
        /// A complete URL of your website. It will be used by the gateway for redirecting the
        /// client again to your website.
        /// <para>Note: A complete URL would be like: "http://www.mywebsite.com/foo/bar/"</para>
        /// </param>
        IInvoiceBuilder SetCallbackUrl(CallbackUrl url);

        /// <summary>
        /// Sets the type of the gateway which the invoice must be paid in.
        /// </summary>
        /// <param name="gatewayType">Type of the gateway</param>
        /// <exception cref="InvalidGatewayTypeException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        IInvoiceBuilder SetGatewayType(Type gatewayType);

        /// <summary>
        /// Adds additional data to the invoice.
        /// </summary>
        /// <param name="key">Key of the data</param>
        /// <param name="value">Value of the data</param>
        IInvoiceBuilder AddAdditionalData(string key, object value);

        /// <summary>
        /// Builds an invoice with the given data.
        /// </summary>
        Task<Invoice> BuildAsync(CancellationToken cancellationToken = default);
    }
}
