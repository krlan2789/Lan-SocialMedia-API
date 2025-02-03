namespace LanGeng.API.Helper;

public static class EmailHelper
{
    public static string CreateHtmlWithLink(string receiptName, string title, string content, string verificationLink)
    {
        string emailTemplate = $@"
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f0f3ff;
                    margin: 0;
                    padding: 0;
                }}
                .container {{
                    width: 100%;
                    max-width: 512px;
                    margin: 0 auto;
                    background-color: #f0f3ff;
                    padding: 20px;
                    border-radius: 16px;
                }}
                .header {{
                    text-align: center;
                    padding-bottom: 16px;
                }}
                .content {{
                    font-size: 16px;
                    line-height: 1.6;
                    color: #344155;
                }}
                .button {{
                    display: inline-block;
                    background-color: #79688e;
                    color: #f0f3ff;
                    padding: 8px 16px;
                    text-align: center;
                    border-radius: 8px;
                    text-decoration: none;
                    margin: 16px 0;
                }}
                .footer {{
                    text-align: center;
                    font-size: 12px;
                    color: #344155;
                    padding-top: 16px;
                }}
            </style>
            <div class='container'>
                <div class='content'>
                    <h1>{title}</h1>
                    <p>{(string.IsNullOrEmpty(receiptName) ? "" : $"Hello {receiptName},")}</p>
                    <p>{content}</p>
                    <a href='{verificationLink}' class='button'>Verify</a>
                    <p>If you did not create an account, please disregard this email.</p>
                    <p>Best regards,<br>LanGeng</p>
                </div>
                <div class='footer'>
                    <p>&copy;2025 LanGeng. All rights reserved.</p>
                </div>
            </div>
        ";
        return emailTemplate;
    }

    public static string CreateHtmlWithCode(string receiptName, string title, string content, string code)
    {
        string emailTemplate = $@"
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f0f3ff;
                    margin: 0;
                    padding: 0;
                }}
                .container {{
                    width: 100%;
                    max-width: 512px;
                    margin: 0 auto;
                    background-color: #f0f3ff;
                    padding: 20px;
                    border-radius: 16px;
                }}
                .header {{
                    text-align: center;
                    padding-bottom: 16px;
                }}
                .content {{
                    font-size: 16px;
                    line-height: 1.6;
                    color: #344155;
                }}
                .code {{
                    font-size: 32px;
                    background-color: #79688e;
                    color: #f0f3ff;
                    padding: 8px 16px;
                    text-align: center;
                    border-radius: 8px;
                    margin: 24px 0;
                }}
                .footer {{
                    text-align: center;
                    font-size: 12px;
                    color: #344155;
                    padding-top: 16px;
                }}
            </style>
            <div class='container'>
                <div class='content'>
                    <h1>{title}</h1>
                    <p>{(string.IsNullOrEmpty(receiptName) ? "" : $"Hello {receiptName},")}</p>
                    <p>{content}</p>
                    <code class='code'>{code}</code>
                    <p>If you did not create an account, please disregard this email.</p>
                    <p>Best regards,<br>LanGeng</p>
                </div>
                <div class='footer'>
                    <p>&copy;2025 LanGeng. All rights reserved.</p>
                </div>
            </div>
        ";
        return emailTemplate;
    }
}
