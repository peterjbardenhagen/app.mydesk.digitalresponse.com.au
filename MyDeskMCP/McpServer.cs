using System.Text.Json;
using Techlight.MyDesk.MCP.Models;
using Techlight.MyDesk.MCP.Services;

namespace Techlight.MyDesk.MCP;

public class McpServer
{
    private readonly ILogger<McpServer> _logger;
    private readonly QuoteService _quoteService;
    private readonly InvoiceService _invoiceService;
    private readonly PurchaseOrderService _poService;
    private readonly ContactService _contactService;
    private readonly UserService _userService;
    private readonly EmailService _emailService;

    public McpServer(
        ILogger<McpServer> logger,
        QuoteService quoteService,
        InvoiceService invoiceService,
        PurchaseOrderService poService,
        ContactService contactService,
        UserService userService,
        EmailService emailService)
    {
        _logger = logger;
        _quoteService = quoteService;
        _invoiceService = invoiceService;
        _poService = poService;
        _contactService = contactService;
        _userService = userService;
        _emailService = emailService;
    }

    public List<McpTool> GetAvailableTools()
    {
        return new List<McpTool>
        {
            // Quote Tools
            new McpTool
            {
                Name = "get_quote",
                Description = "Get detailed information about a specific quote by ID",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["quote_id"] = new McpToolProperty { Type = "integer", Description = "The quote ID number" }
                    },
                    Required = new List<string> { "quote_id" }
                }
            },
            new McpTool
            {
                Name = "list_quotes",
                Description = "List quotes with optional filters (date range, customer, status)",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["date_from"] = new McpToolProperty { Type = "string", Description = "Start date (YYYY-MM-DD)" },
                        ["date_to"] = new McpToolProperty { Type = "string", Description = "End date (YYYY-MM-DD)" },
                        ["customer_name"] = new McpToolProperty { Type = "string", Description = "Customer name to filter by" },
                        ["status"] = new McpToolProperty { Type = "string", Description = "Quote status filter" },
                        ["limit"] = new McpToolProperty { Type = "integer", Description = "Maximum number of results (default 50)" }
                    },
                    Required = new List<string>()
                }
            },
            new McpTool
            {
                Name = "create_quote",
                Description = "Create a new quote with line items",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["contact_id"] = new McpToolProperty { Type = "integer", Description = "Contact ID for the customer" },
                        ["reference"] = new McpToolProperty { Type = "string", Description = "Quote reference/title" },
                        ["division_id"] = new McpToolProperty { Type = "integer", Description = "Division ID" },
                        ["line_items"] = new McpToolProperty { Type = "string", Description = "JSON array of line items with description, quantity, unit_cost, unit_price" },
                        ["customer_notes"] = new McpToolProperty { Type = "string", Description = "Notes for the customer" },
                        ["internal_notes"] = new McpToolProperty { Type = "string", Description = "Internal notes" }
                    },
                    Required = new List<string> { "contact_id", "reference", "division_id", "line_items" }
                }
            },
            new McpTool
            {
                Name = "update_quote_status",
                Description = "Update the status of a quote",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["quote_id"] = new McpToolProperty { Type = "integer", Description = "Quote ID" },
                        ["status"] = new McpToolProperty { Type = "string", Description = "New status (Draft, Pending, Submitted, Won, Lost, etc.)" },
                        ["notes"] = new McpToolProperty { Type = "string", Description = "Optional notes about the status change" }
                    },
                    Required = new List<string> { "quote_id", "status" }
                }
            },
            new McpTool
            {
                Name = "email_quote",
                Description = "Email a quote to a recipient",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["quote_id"] = new McpToolProperty { Type = "integer", Description = "Quote ID to email" },
                        ["to_email"] = new McpToolProperty { Type = "string", Description = "Recipient email address" },
                        ["subject"] = new McpToolProperty { Type = "string", Description = "Email subject (optional)" },
                        ["message"] = new McpToolProperty { Type = "string", Description = "Custom message (optional)" },
                        ["include_pdf"] = new McpToolProperty { Type = "boolean", Description = "Include PDF attachment (default true)" }
                    },
                    Required = new List<string> { "quote_id", "to_email" }
                }
            },
            new McpTool
            {
                Name = "generate_quote_report",
                Description = "Generate a report of quotes for a date range",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["date_from"] = new McpToolProperty { Type = "string", Description = "Start date (YYYY-MM-DD)" },
                        ["date_to"] = new McpToolProperty { Type = "string", Description = "End date (YYYY-MM-DD)" },
                        ["originator_code"] = new McpToolProperty { Type = "string", Description = "Filter by user code" },
                        ["customer_name"] = new McpToolProperty { Type = "string", Description = "Filter by customer name" }
                    },
                    Required = new List<string> { "date_from", "date_to" }
                }
            },

            // Invoice Tools
            new McpTool
            {
                Name = "get_invoice",
                Description = "Get detailed information about a specific invoice",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["invoice_id"] = new McpToolProperty { Type = "integer", Description = "The invoice ID" }
                    },
                    Required = new List<string> { "invoice_id" }
                }
            },
            new McpTool
            {
                Name = "list_invoices",
                Description = "List invoices with optional filters",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["date_from"] = new McpToolProperty { Type = "string", Description = "Start date (YYYY-MM-DD)" },
                        ["date_to"] = new McpToolProperty { Type = "string", Description = "End date (YYYY-MM-DD)" },
                        ["customer_name"] = new McpToolProperty { Type = "string", Description = "Customer name filter" },
                        ["status"] = new McpToolProperty { Type = "string", Description = "Invoice status" },
                        ["quote_id"] = new McpToolProperty { Type = "integer", Description = "Filter by related quote ID" },
                        ["limit"] = new McpToolProperty { Type = "integer", Description = "Maximum results (default 50)" }
                    },
                    Required = new List<string>()
                }
            },
            new McpTool
            {
                Name = "get_latest_invoices",
                Description = "Get invoices from the last N days",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["days"] = new McpToolProperty { Type = "integer", Description = "Number of days (default 30)" }
                    },
                    Required = new List<string>()
                }
            },
            new McpTool
            {
                Name = "generate_invoice_report",
                Description = "Generate a report of invoices",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["date_from"] = new McpToolProperty { Type = "string", Description = "Start date (YYYY-MM-DD)" },
                        ["date_to"] = new McpToolProperty { Type = "string", Description = "End date (YYYY-MM-DD)" },
                        ["customer_name"] = new McpToolProperty { Type = "string", Description = "Filter by customer" },
                        ["originator_code"] = new McpToolProperty { Type = "string", Description = "Filter by originator" }
                    },
                    Required = new List<string> { "date_from", "date_to" }
                }
            },

            // Purchase Order Tools
            new McpTool
            {
                Name = "get_purchase_order",
                Description = "Get detailed information about a purchase order",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["po_id"] = new McpToolProperty { Type = "integer", Description = "Purchase Order ID" }
                    },
                    Required = new List<string> { "po_id" }
                }
            },
            new McpTool
            {
                Name = "list_purchase_orders",
                Description = "List purchase orders with filters",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["date_from"] = new McpToolProperty { Type = "string", Description = "Start date (YYYY-MM-DD)" },
                        ["date_to"] = new McpToolProperty { Type = "string", Description = "End date (YYYY-MM-DD)" },
                        ["supplier_name"] = new McpToolProperty { Type = "string", Description = "Supplier name filter" },
                        ["status"] = new McpToolProperty { Type = "string", Description = "PO status" },
                        ["quote_id"] = new McpToolProperty { Type = "integer", Description = "Related quote ID" }
                    },
                    Required = new List<string>()
                }
            },
            new McpTool
            {
                Name = "update_po_status",
                Description = "Update purchase order status",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["po_id"] = new McpToolProperty { Type = "integer", Description = "Purchase Order ID" },
                        ["status"] = new McpToolProperty { Type = "string", Description = "New status (Draft, Pending, Ordered, Partially Received, Received, Cancelled, Completed)" },
                        ["notes"] = new McpToolProperty { Type = "string", Description = "Optional notes" }
                    },
                    Required = new List<string> { "po_id", "status" }
                }
            },

            // Contact Tools
            new McpTool
            {
                Name = "get_contact",
                Description = "Get contact details by ID",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["contact_id"] = new McpToolProperty { Type = "integer", Description = "Contact ID" }
                    },
                    Required = new List<string> { "contact_id" }
                }
            },
            new McpTool
            {
                Name = "search_contacts",
                Description = "Search for contacts by name",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["name"] = new McpToolProperty { Type = "string", Description = "Name to search for" },
                        ["company_name"] = new McpToolProperty { Type = "string", Description = "Company name filter" },
                        ["limit"] = new McpToolProperty { Type = "integer", Description = "Maximum results (default 10)" }
                    },
                    Required = new List<string> { "name" }
                }
            },

            // User Tools
            new McpTool
            {
                Name = "get_user_info",
                Description = "Get current user information",
                InputSchema = new McpToolSchema
                {
                    Properties = new Dictionary<string, McpToolProperty>(),
                    Required = new List<string>()
                }
            }
        };
    }

    public async Task<McpResponse> HandleToolCallAsync(McpToolCallRequest request, McpContext context)
    {
        try
        {
            object? result = request.Name switch
            {
                // Quote Tools
                "get_quote" => await HandleGetQuoteAsync(request.Arguments, context),
                "list_quotes" => await HandleListQuotesAsync(request.Arguments, context),
                "create_quote" => await HandleCreateQuoteAsync(request.Arguments, context),
                "update_quote_status" => await HandleUpdateQuoteStatusAsync(request.Arguments, context),
                "email_quote" => await HandleEmailQuoteAsync(request.Arguments, context),
                "generate_quote_report" => await HandleGenerateQuoteReportAsync(request.Arguments, context),

                // Invoice Tools
                "get_invoice" => await HandleGetInvoiceAsync(request.Arguments, context),
                "list_invoices" => await HandleListInvoicesAsync(request.Arguments, context),
                "get_latest_invoices" => await HandleGetLatestInvoicesAsync(request.Arguments, context),
                "generate_invoice_report" => await HandleGenerateInvoiceReportAsync(request.Arguments, context),

                // Purchase Order Tools
                "get_purchase_order" => await HandleGetPurchaseOrderAsync(request.Arguments, context),
                "list_purchase_orders" => await HandleListPurchaseOrdersAsync(request.Arguments, context),
                "update_po_status" => await HandleUpdatePOStatusAsync(request.Arguments, context),

                // Contact Tools
                "get_contact" => await HandleGetContactAsync(request.Arguments, context),
                "search_contacts" => await HandleSearchContactsAsync(request.Arguments, context),

                // User Tools
                "get_user_info" => await HandleGetUserInfoAsync(context),

                _ => throw new ArgumentException($"Unknown tool: {request.Name}")
            };

            return new McpResponse
            {
                Result = new McpToolCallResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent
                        {
                            Type = "text",
                            Text = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", request.Name);
            
            return new McpResponse
            {
                Error = new McpError
                {
                    Code = -32603,
                    Message = $"Internal error: {ex.Message}"
                }
            };
        }
    }

    // Quote Handlers
    private async Task<object?> HandleGetQuoteAsync(Dictionary<string, object> args, McpContext context)
    {
        var quoteId = Convert.ToInt32(args["quote_id"]);
        var quote = await _quoteService.GetQuoteByIdAsync(quoteId, context);
        
        if (quote == null) return new { error = "Quote not found" };
        
        // Get line items
        var lineItems = await _quoteService.GetQuoteLineItemsAsync(quoteId);
        
        return new { quote, line_items = lineItems };
    }

    private async Task<object?> HandleListQuotesAsync(Dictionary<string, object> args, McpContext context)
    {
        DateTime? dateFrom = args.TryGetValue("date_from", out var df) ? DateTime.Parse(df.ToString()!) : null;
        DateTime? dateTo = args.TryGetValue("date_to", out var dt) ? DateTime.Parse(dt.ToString()!) : null;
        string? customerName = args.TryGetValue("customer_name", out var cn) ? cn.ToString() : null;
        string? status = args.TryGetValue("status", out var s) ? s.ToString() : null;
        int? limit = args.TryGetValue("limit", out var l) ? Convert.ToInt32(l) : 50;

        var quotes = await _quoteService.GetQuotesAsync(dateFrom, dateTo, customerName, status, null, limit, context);
        return new { count = quotes.Count, quotes };
    }

    private async Task<object?> HandleCreateQuoteAsync(Dictionary<string, object> args, McpContext context)
    {
        var request = new CreateQuoteRequest
        {
            ContactId = Convert.ToInt32(args["contact_id"]),
            Reference = args["reference"].ToString()!,
            DivisionId = Convert.ToInt32(args["division_id"]),
            CustomerNotes = args.TryGetValue("customer_notes", out var cn) ? cn.ToString() : null,
            InternalNotes = args.TryGetValue("internal_notes", out var inn) ? inn.ToString() : null
        };

        // Parse line items from JSON string
        if (args.TryGetValue("line_items", out var li))
        {
            var lineItemsJson = li.ToString()!;
            request.LineItems = JsonSerializer.Deserialize<List<QuoteLineItemRequest>>(lineItemsJson) ?? new();
        }

        var quote = await _quoteService.CreateQuoteAsync(request, context);
        return new { success = true, quote_id = quote.Qid, message = $"Quote {quote.Qid} created successfully" };
    }

    private async Task<object?> HandleUpdateQuoteStatusAsync(Dictionary<string, object> args, McpContext context)
    {
        var quoteId = Convert.ToInt32(args["quote_id"]);
        var status = args["status"].ToString()!;
        string? notes = args.TryGetValue("notes", out var n) ? n.ToString() : null;

        var quote = await _quoteService.UpdateQuoteStatusAsync(quoteId, status, notes, context);
        return new { success = true, quote_id = quote.Qid, new_status = quote.QuoteStatus };
    }

    private async Task<object?> HandleEmailQuoteAsync(Dictionary<string, object> args, McpContext context)
    {
        var quoteId = Convert.ToInt32(args["quote_id"]);
        var toEmail = args["to_email"].ToString()!;
        string? subject = args.TryGetValue("subject", out var s) ? s.ToString() : null;
        string? message = args.TryGetValue("message", out var m) ? m.ToString() : null;
        bool includePdf = args.TryGetValue("include_pdf", out var ip) ? Convert.ToBoolean(ip) : true;

        var success = await _emailService.EmailQuoteAsync(quoteId, toEmail, subject, message, includePdf, context);
        return new { success, message = success ? $"Quote {quoteId} emailed to {toEmail}" : "Failed to send email" };
    }

    private async Task<object?> HandleGenerateQuoteReportAsync(Dictionary<string, object> args, McpContext context)
    {
        var dateFrom = DateTime.Parse(args["date_from"].ToString()!);
        var dateTo = DateTime.Parse(args["date_to"].ToString()!);
        string? originatorCode = args.TryGetValue("originator_code", out var oc) ? oc.ToString() : null;
        string? customerName = args.TryGetValue("customer_name", out var cn) ? cn.ToString() : null;

        var report = await _quoteService.GenerateQuoteReportAsync(dateFrom, dateTo, originatorCode, customerName, context);
        return report;
    }

    // Invoice Handlers
    private async Task<object?> HandleGetInvoiceAsync(Dictionary<string, object> args, McpContext context)
    {
        var invoiceId = Convert.ToInt32(args["invoice_id"]);
        var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId, context);
        return invoice != null ? invoice : new { error = "Invoice not found" };
    }

    private async Task<object?> HandleListInvoicesAsync(Dictionary<string, object> args, McpContext context)
    {
        DateTime? dateFrom = args.TryGetValue("date_from", out var df) ? DateTime.Parse(df.ToString()!) : null;
        DateTime? dateTo = args.TryGetValue("date_to", out var dt) ? DateTime.Parse(dt.ToString()!) : null;
        string? customerName = args.TryGetValue("customer_name", out var cn) ? cn.ToString() : null;
        string? status = args.TryGetValue("status", out var s) ? s.ToString() : null;
        int? quoteId = args.TryGetValue("quote_id", out var qid) ? Convert.ToInt32(qid) : null;
        int? limit = args.TryGetValue("limit", out var l) ? Convert.ToInt32(l) : 50;

        var invoices = await _invoiceService.GetInvoicesAsync(dateFrom, dateTo, customerName, null, status, quoteId, limit, context);
        return new { count = invoices.Count, invoices };
    }

    private async Task<object?> HandleGetLatestInvoicesAsync(Dictionary<string, object> args, McpContext context)
    {
        int days = args.TryGetValue("days", out var d) ? Convert.ToInt32(d) : 30;
        var invoices = await _invoiceService.GetLatestInvoicesAsync(days, context);
        return new { days, count = invoices.Count, invoices };
    }

    private async Task<object?> HandleGenerateInvoiceReportAsync(Dictionary<string, object> args, McpContext context)
    {
        var dateFrom = DateTime.Parse(args["date_from"].ToString()!);
        var dateTo = DateTime.Parse(args["date_to"].ToString()!);
        string? customerName = args.TryGetValue("customer_name", out var cn) ? cn.ToString() : null;
        string? originatorCode = args.TryGetValue("originator_code", out var oc) ? oc.ToString() : null;

        var request = new InvoiceReportRequest
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            CustomerName = customerName,
            OriginatorCode = originatorCode
        };

        var report = await _invoiceService.GenerateInvoiceReportAsync(request, context);
        return report;
    }

    // Purchase Order Handlers
    private async Task<object?> HandleGetPurchaseOrderAsync(Dictionary<string, object> args, McpContext context)
    {
        var poId = Convert.ToInt32(args["po_id"]);
        var po = await _poService.GetPurchaseOrderByIdAsync(poId, context);
        return po != null ? po : new { error = "Purchase Order not found" };
    }

    private async Task<object?> HandleListPurchaseOrdersAsync(Dictionary<string, object> args, McpContext context)
    {
        DateTime? dateFrom = args.TryGetValue("date_from", out var df) ? DateTime.Parse(df.ToString()!) : null;
        DateTime? dateTo = args.TryGetValue("date_to", out var dt) ? DateTime.Parse(dt.ToString()!) : null;
        string? supplierName = args.TryGetValue("supplier_name", out var sn) ? sn.ToString() : null;
        string? status = args.TryGetValue("status", out var s) ? s.ToString() : null;
        int? quoteId = args.TryGetValue("quote_id", out var qid) ? Convert.ToInt32(qid) : null;

        var pos = await _poService.GetPurchaseOrdersAsync(dateFrom, dateTo, supplierName, status, null, quoteId, 50, context);
        return new { count = pos.Count, purchase_orders = pos };
    }

    private async Task<object?> HandleUpdatePOStatusAsync(Dictionary<string, object> args, McpContext context)
    {
        var poId = Convert.ToInt32(args["po_id"]);
        var status = args["status"].ToString()!;
        string? notes = args.TryGetValue("notes", out var n) ? n.ToString() : null;

        var po = await _poService.UpdatePurchaseOrderStatusAsync(poId, status, notes, context);
        return new { success = true, po_id = po.PurchaseOrderId, new_status = po.Status };
    }

    // Contact Handlers
    private async Task<object?> HandleGetContactAsync(Dictionary<string, object> args, McpContext context)
    {
        var contactId = Convert.ToInt32(args["contact_id"]);
        var contact = await _contactService.GetContactByIdAsync(contactId, context);
        return contact != null ? contact : new { error = "Contact not found" };
    }

    private async Task<object?> HandleSearchContactsAsync(Dictionary<string, object> args, McpContext context)
    {
        var name = args["name"].ToString()!;
        string? companyName = args.TryGetValue("company_name", out var cn) ? cn.ToString() : null;
        int? limit = args.TryGetValue("limit", out var l) ? Convert.ToInt32(l) : 10;

        var contacts = await _contactService.SearchContactsByNameAsync(name, context);
        return new { count = contacts.Count, contacts = contacts.Take(limit ?? 10) };
    }

    // User Handler
    private async Task<object?> HandleGetUserInfoAsync(McpContext context)
    {
        return new
        {
            user_code = context.UserCode,
            user_name = context.UserName,
            is_admin = context.IsAdmin,
            accessible_divisions = context.AccessibleDivisions,
            request_time = context.RequestTime
        };
    }
}
