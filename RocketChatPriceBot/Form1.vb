Imports System.IO
Imports System.Net
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports Rocket.Chat.Net.Driver
Imports Rocket.Chat.Net.Interfaces
Imports Rocket.Chat.Net.Models.LoginOptions

Public Class Form1
    Dim loginOption As ILoginOption
    Dim driver As IRocketChatDriver
    Private Async Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim ErrorMessage As String = "The following fields are empty:" & vbCrLf
        Dim ErrorsFound As Boolean = False
        If String.IsNullOrEmpty(Server.Text) Then
            ErrorMessage += "-Server" & vbCrLf
            ErrorsFound = True
        End If
        If String.IsNullOrEmpty(UserName.Text) Then
            ErrorMessage += "-Username" & vbCrLf
            ErrorsFound = True
        End If
        If String.IsNullOrEmpty(Password.Text) Then
            ErrorMessage += "-Password" & vbCrLf
            ErrorsFound = True
        End If
        If String.IsNullOrEmpty(RoomName.Text) Then
            ErrorMessage += "-Room Name" & vbCrLf
            ErrorsFound = True
        End If
        If ErrorsFound = False Then
            My.Settings.Server = Server.Text
            My.Settings.Username = UserName.Text
            My.Settings.Password = Password.Text
            My.Settings.Room = RoomName.Text
            My.Settings.Save()
            loginOption = New LdapLoginOption() With {.Username = UserName.Text, .Password = Password.Text}
            driver = New RocketChatDriver(Server.Text, True)
            Await driver.ConnectAsync()
            Await driver.LoginAsync(loginOption)
            BackgroundWorker1.RunWorkerAsync()
        Else
            ErrorMessage += vbCrLf & "Please fill the above fields to start using this Bot"
            MsgBox(ErrorMessage)
        End If

    End Sub

    Dim RoomIDResult
    Private Async Sub BackgroundWorker1_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        Dim getBotsChannelID = Await driver.GetRoomIdAsync(RoomName.Text)
        RoomIDResult = getBotsChannelID.Result
        Await Driver.SubscribeToRoomAsync(RoomIDResult)
        Dim messages As Object
        Dim FirstMessageToString As String
        While True
            Try
                messages = Await Driver.LoadMessagesAsync(RoomIDResult)
                FirstMessageToString = messages.Result.Messages.Item(0).ToString
                If FirstMessageToString.ToLower.Contains("!value") Or FirstMessageToString.ToLower.Contains("!price") Then
                    Dim SplitWords As String() = FirstMessageToString.Split(" ")
                    If SplitWords.Count > 2 Then
                        Dim i = 0
                        For Each word In SplitWords
                            If word = "!value" Or word = "!price" Then
                                GetPrice(SplitWords(i + 1))
                            End If
                            i = i + 1
                        Next
                        Await driver.SendMessageAsync(CurrencyReply, RoomIDResult)
                        CurrencyReply = ""
                    Else
                        GetPrice("steem")
                        Await driver.SendMessageAsync(CurrencyReply, RoomIDResult)
                    End If
                End If
                If FirstMessageToString.ToLower.Contains("!calculate") Then
                    Dim SplitWords As String() = FirstMessageToString.Split(" ")
                    If SplitWords.Count > 2 Then
                        Dim i = 0
                        For Each word In SplitWords
                            If word = "!calculate" Then
                                CalculatePrice(SplitWords(i + 2), SplitWords(i + 1))
                            End If
                            i = i + 1
                        Next
                        Await driver.SendMessageAsync(CurrencyReply, RoomIDResult)
                        CurrencyReply = ""
                    Else
                        GetPrice("steem")
                        Await driver.SendMessageAsync(CurrencyReply, RoomIDResult)
                    End If
                End If
                Threading.Thread.Sleep(1000)
            Catch ex As Exception

            End Try
        End While
    End Sub

    Dim CurrencyReply As String = ""
    Private Sub GetPrice(Currency As String)
        Dim request As WebRequest = WebRequest.Create("https://api.coinmarketcap.com/v1/ticker/")
        Dim response As WebResponse = request.GetResponse()
        Console.WriteLine(CType(response, HttpWebResponse).StatusDescription)
        Dim dataStream As Stream = response.GetResponseStream()
        Dim reader As New StreamReader(dataStream)
        Dim responseFromServer As String = reader.ReadToEnd()
        Dim result = JsonConvert.DeserializeObject(Of ArrayList)(responseFromServer)
        Dim token As JToken
        Dim id As String = ""
        Dim name As String = ""
        Dim symbol As String = ""
        Dim price_usd As String = ""
        Dim price_btc As String = ""
        Dim percent_1h As String = ""
        Dim percent_24h As String = ""
        Dim percent_7d As String = ""
        For Each value As Object In result
            token = JObject.Parse(value.ToString())
            id = token.SelectToken("id")
            name = token.SelectToken("name")
            symbol = token.SelectToken("symbol")
            If id.ToLower = Currency.ToLower Or name.ToLower = Currency.ToLower Or symbol.ToLower = Currency.ToLower Then
                price_usd = token.SelectToken("price_usd")
                price_btc = token.SelectToken("price_btc")
                percent_1h = token.SelectToken("percent_change_1h")
                percent_24h = token.SelectToken("percent_change_24h")
                percent_7d = token.SelectToken("percent_change_7d")
                Exit For
            End If
        Next
        If price_btc = "" = False Then
            CurrencyReply = "The price of " & name & " is " & price_btc & " BTC and $" & price_usd & " USD. Percent Change during the Last hour: " & percent_1h & "%, Last 24 hours: " & percent_24h & "%, Last 7 days: " & percent_7d & "%. Source: CoinMarketCap"
        Else
            CurrencyReply = "Cannot find the value of " & Currency & "."
        End If
        reader.Close()
        response.Close()
    End Sub
    Private Sub CalculatePrice(Currency As String, Amount As Double)
        Dim request As WebRequest = WebRequest.Create("https://api.coinmarketcap.com/v1/ticker/")
        Dim response As WebResponse = request.GetResponse()
        Console.WriteLine(CType(response, HttpWebResponse).StatusDescription)
        Dim dataStream As Stream = response.GetResponseStream()
        Dim reader As New StreamReader(dataStream)
        Dim responseFromServer As String = reader.ReadToEnd()
        Dim result = JsonConvert.DeserializeObject(Of ArrayList)(responseFromServer)
        Dim token As JToken
        Dim id As String = ""
        Dim name As String = ""
        Dim symbol As String = ""
        Dim price_usd As String = ""
        Dim price_btc As String = ""
        For Each value As Object In result
            token = JObject.Parse(value.ToString())
            id = token.SelectToken("id")
            name = token.SelectToken("name")
            symbol = token.SelectToken("symbol")
            If id.ToLower = Currency.ToLower Or name.ToLower = Currency.ToLower Or symbol.ToLower = Currency.ToLower Then
                price_usd = token.SelectToken("price_usd")
                price_btc = token.SelectToken("price_btc")
                Exit For
            End If
        Next
        If price_btc = "" = False Then
            Dim BTCCalc = Amount * price_btc
            Dim USDCalc = Amount * price_usd
            CurrencyReply = Amount & " " & name & " equals to " & BTCCalc & " BTC and $" & USDCalc & " USD. Based on the current price on CoinMarketCap"
        Else
            CurrencyReply = "Cannot find the currency " & Currency & "."
        End If
        reader.Close()
        response.Close()
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Server.Text = My.Settings.Server
        UserName.Text = My.Settings.Username
        Password.Text = My.Settings.Password
        RoomName.Text = My.Settings.Room
    End Sub
End Class
