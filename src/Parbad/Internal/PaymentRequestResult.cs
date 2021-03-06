// Copyright (c) Parbad. All rights reserved.
// Licensed under the GNU GENERAL PUBLIC License, Version 3.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Parbad.Internal
{
    public class PaymentRequestResult : PaymentResult, IPaymentRequestResult
    {
        public IGatewayTransporter GatewayTransporter { get; set; }

        public static PaymentRequestResult Succeed(IGatewayTransporter gatewayTransporter)
        {
            return new PaymentRequestResult
            {
                IsSucceed = true,
                GatewayTransporter = gatewayTransporter
            };
        }

        public static PaymentRequestResult Failed(string message)
        {
            return new PaymentRequestResult
            {
                IsSucceed = false,
                Message = message,
                GatewayTransporter = new NullGatewayTransporter()
            };
        }
    }
}
