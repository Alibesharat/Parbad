// Copyright (c) Parbad. All rights reserved.
// Licensed under the GNU GENERAL PUBLIC License, Version 3.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Parbad.Abstraction;
using Parbad.Data.Domain.Payments;
using Parbad.Data.Domain.Transactions;
using Parbad.GatewayProviders.Parsian.Models;
using Parbad.Internal;
using Parbad.Options;
using Parbad.Utilities;

namespace Parbad.GatewayProviders.Parsian
{
    internal static class ParsianHelper
    {
        private const string PaymentPageUrl = "https://pec.shaparak.ir/NewIPG/";
        public const string BaseServiceUrl = "https://pec.shaparak.ir/";
        public const string RequestServiceUrl = "/NewIPGServices/Sale/SaleService.asmx";
        public const string VerifyServiceUrl = "/NewIPGServices/Confirm/ConfirmService.asmx";
        public const string RefundServiceUrl = "/NewIPGServices/Reverse/ReversalService.asmx";

        public static string CreateRequestData(ParsianGatewayOptions options, Invoice invoice)
        {
            return
                "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:sal=\"https://pec.Shaparak.ir/NewIPGServices/Sale/SaleService\">" +
                "<soapenv:Header/>" +
                "<soapenv:Body>" +
                "<sal:SalePaymentRequest>" +
                "<!--Optional:-->" +
                "<sal:requestData>" +
                "<!--Optional:-->" +
                $"<sal:LoginAccount>{options.LoginAccount}</sal:LoginAccount>" +
                $"<sal:Amount>{(long)invoice.Amount}</sal:Amount>" +
                $"<sal:OrderId>{invoice.TrackingNumber}</sal:OrderId>" +
                "<!--Optional:-->" +
                $"<sal:CallBackUrl>{invoice.CallbackUrl}</sal:CallBackUrl>" +
                "<!--Optional:-->" +
                "<sal:AdditionalData></sal:AdditionalData>" +
                "<!--Optional:-->" +
                "<sal:Originator></sal:Originator>" +
                "</sal:requestData>" +
                "</sal:SalePaymentRequest> " +
                "</soapenv:Body> " +
                "</soapenv:Envelope> ";
        }

        public static PaymentRequestResult CreateRequestResult(string webServiceResponse, IHttpContextAccessor httpContextAccessor, MessagesOptions messagesOptions)
        {
            var token = XmlHelper.GetNodeValueFromXml(webServiceResponse, "Token", "https://pec.Shaparak.ir/NewIPGServices/Sale/SaleService");
            var status = XmlHelper.GetNodeValueFromXml(webServiceResponse, "Status", "https://pec.Shaparak.ir/NewIPGServices/Sale/SaleService");
            var message = XmlHelper.GetNodeValueFromXml(webServiceResponse, "Message", "https://pec.Shaparak.ir/NewIPGServices/Sale/SaleService");

            var isSucceed = !status.IsNullOrEmpty() &&
                            status == "0" &&
                            !token.IsNullOrEmpty();

            if (!isSucceed)
            {
                if (message == null)
                {
                    message = messagesOptions.PaymentFailed;
                }

                return PaymentRequestResult.Failed(message);
            }

            var paymentPageUrl = $"{PaymentPageUrl}?Token={token}";

            var result = new PaymentRequestResult
            {
                IsSucceed = true,
                GatewayTransporter = new GatewayRedirect(httpContextAccessor, paymentPageUrl)
            };

            result.DatabaseAdditionalData.Add("token", token);

            return result;
        }

        public static ParsianCallbackResult CreateCallbackResult(Payment payment, HttpRequest httpRequest, MessagesOptions messagesOptions)
        {
            httpRequest.Form.TryGetValue("token", out var token);
            httpRequest.Form.TryGetValue("status", out var status);
            httpRequest.Form.TryGetValue("orderId", out var orderId);
            httpRequest.Form.TryGetValue("amount", out var amount);
            httpRequest.Form.TryGetValue("RRN", out var rrn);

            var isSucceed = !status.IsNullOrEmpty() &&
                            status == "0" &&
                            !token.IsNullOrEmpty();

            string message = null;

            if (isSucceed)
            {
                if (rrn.IsNullOrEmpty() ||
                    amount.IsNullOrEmpty() ||
                    orderId.IsNullOrEmpty() ||
                    !long.TryParse(orderId, out var numberOrderNumber) ||
                    !long.TryParse(amount, out var numberAmount) ||
                    numberOrderNumber != payment.TrackingNumber ||
                    numberAmount != (long)payment.Amount)
                {
                    isSucceed = false;
                    message = messagesOptions.InvalidDataReceivedFromGateway;
                }
            }
            else
            {
                message = $"Error {status}";
            }

            PaymentVerifyResult verifyResult = null;

            if (!isSucceed)
            {
                verifyResult = PaymentVerifyResult.Failed(message);
            }

            return new ParsianCallbackResult
            {
                IsSucceed = isSucceed,
                RRN = rrn,
                Result = verifyResult
            };
        }

        public static string CreateVerifyData(ParsianGatewayOptions options, ParsianCallbackResult callbackResult)
        {
            return
                "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:con=\"https://pec.Shaparak.ir/NewIPGServices/Confirm/ConfirmService\">" +
                "<soapenv:Header/>" +
                "<soapenv:Body>" +
                "<con:ConfirmPayment>" +
                "<!--Optional:-->" +
                "<con:requestData>" +
                "<!--Optional:-->" +
                $"<con:LoginAccount>{options.LoginAccount}</con:LoginAccount>" +
                $"<con:Token>{callbackResult.Token}</con:Token>" +
                "</con:requestData>" +
                "</con:ConfirmPayment>" +
                "</soapenv:Body>" +
                "</soapenv:Envelope>";
        }

        public static PaymentVerifyResult CreateVerifyResult(string webServiceResponse, ParsianCallbackResult callbackResult, MessagesOptions messagesOptions)
        {
            var status = XmlHelper.GetNodeValueFromXml(webServiceResponse, "Status", "https://pec.Shaparak.ir/NewIPGServices/Confirm/ConfirmService");
            var rrn = XmlHelper.GetNodeValueFromXml(webServiceResponse, "RRN", "https://pec.Shaparak.ir/NewIPGServices/Confirm/ConfirmService");
            var token = XmlHelper.GetNodeValueFromXml(webServiceResponse, "Token", "https://pec.Shaparak.ir/NewIPGServices/Confirm/ConfirmService");

            var isSucceed = !status.IsNullOrEmpty() && status == "0";

            var message = isSucceed
                ? messagesOptions.PaymentSucceed
                : $"Error {status}";

            var result = new PaymentVerifyResult
            {
                IsSucceed = isSucceed,
                TransactionCode = rrn,
                Message = message
            };

            result.DatabaseAdditionalData.Add("token", token);

            return result;
        }

        public static string CreateRefundData(ParsianGatewayOptions options, Payment payment, Money amount)
        {
            var transaction = payment.Transactions.SingleOrDefault(item => item.Type == TransactionType.Verify);

            if (transaction == null) throw new InvalidOperationException($"No transaction record found in database for payment with tracking number {payment.TrackingNumber}.");

            if (!AdditionalDataConverter.ToDictionary(transaction).TryGetValue("token", out var token))
            {
                throw new InvalidOperationException($"No token found in database for payment with tracking number {payment.TrackingNumber}.");
            }

            return
                "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:rev=\"https://pec.Shaparak.ir/NewIPGServices/Reversal/ReversalService\">" +
                "<soap:Header/>" +
                "<soap:Body>" +
                "<rev:ReversalRequest>" +
                "<!--Optional:-->" +
                "<rev:requestData>" +
                "<!--Optional:-->" +
                $"<rev:LoginAccount>{options.LoginAccount}</rev:LoginAccount>" +
                $"<rev:Token>{token}</rev:Token>" +
                "</rev:requestData>" +
                "</rev:ReversalRequest>" +
                "</soap:Body>" +
                "</soap:Envelope>";
        }

        public static PaymentRefundResult CreateRefundResult(string webServiceResponse, MessagesOptions messagesOptions)
        {
            var status = XmlHelper.GetNodeValueFromXml(webServiceResponse, "Status", "https://pec.Shaparak.ir/NewIPGServices/Reversal/ReversalService");
            var message = XmlHelper.GetNodeValueFromXml(webServiceResponse, "Message", "https://pec.Shaparak.ir/NewIPGServices/Reversal/ReversalService");
            var token = XmlHelper.GetNodeValueFromXml(webServiceResponse, "Token", "https://pec.Shaparak.ir/NewIPGServices/Reversal/ReversalService");

            if (message.IsNullOrEmpty())
            {
                message = $"Error {status}";
            }

            var result = new PaymentRefundResult
            {
                IsSucceed = status == "0",
                Message = message
            };

            result.DatabaseAdditionalData.Add("token", token);

            return result;
        }
    }
}
