Imports Ayehu.Sdk.ActivityCreation.Interfaces
Imports Ayehu.Sdk.ActivityCreation.Extension
Imports System.Text
Imports System.Diagnostics
Imports Microsoft.VisualBasic
Imports System.IO
Imports System
Imports System.Net
Imports System.Net.Http
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports com.rsa.admin
Imports com.rsa.admin.data
Imports com.rsa.authmgr.admin.agentmgt
Imports com.rsa.authmgr.admin.agentmgt.data
Imports com.rsa.authmgr.admin.hostmgt.data
Imports com.rsa.authmgr.admin.principalmgt
Imports com.rsa.authmgr.admin.principalmgt.data
Imports com.rsa.authmgr.admin.tokenmgt
Imports com.rsa.authn
Imports com.rsa.authn.data
Imports com.rsa.common
Imports com.rsa.command
Imports com.rsa.command.exception
Imports com.rsa.common.search
Imports com.rsa.ucm.am
Imports com.rsa.authmgr.admin.ondemandmgt
Imports System.Data

Namespace Ayehu.Sdk.ActivityCreation
    Public Class CustomActivity
        Implements IActivity

        Public HostName As String
        Public UserName As String
        Public Password As String
        Private URL As String
        Public AccountName As String
        Public SecurityDomain As String
        Private domain As SecurityDomainDTO
        Private idSource As IdentitySourceDTO
        Private dt As DataTable = New DataTable("resultSet")

        Public Function Execute() As ICustomActivityResult Implements IActivity.Execute

            UserName = UserName.Replace(HostName + "\", "")
            URL = "https://" + HostName + ":7002/ims-ws/services/CommandServer"
            dt.Columns.Add("guid")
            dt.Columns.Add("userid")
            dt.Columns.Add("first")
            dt.Columns.Add("last")
            dt.Columns.Add("middle")
            dt.Columns.Add("email")
            dt.Columns.Add("passwordexpired")
            dt.Columns.Add("certificateDN")
            dt.Columns.Add("accountstart")
            dt.Columns.Add("accountexpire")
            dt.Columns.Add("domainGuid")
            dt.Columns.Add("identitySourceGuid")
            dt.Columns.Add("canBeImpersonated")
            dt.Columns.Add("trustToImpersonate")
            dt.Columns.Add("lockoutStatus")
            dt.Columns.Add("emergency lockout status")

            ServicePointManager.Expect100Continue = True
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12

            ServicePointManager.ServerCertificateValidationCallback =
            New RemoteCertificateValidationCallback(AddressOf ValidateServerCertificate)

            Dim conn As New SOAPCommandTarget(URL, UserName, Password)

            If Not conn.Login(UserName, Password) Then Throw New System.Exception("Error: Unable to connect to the remote server. Please make sure your credentials are correct.")

            CommandTargetPolicy.setDefaultCommandTarget(conn)

            Dim searchRealmCmd As New SearchRealmsCommand
            searchRealmCmd.filter = Filter.equal(RealmDTO._NAME_ATTRIBUTE, SecurityDomain)
            searchRealmCmd.execute()

            Dim realms() As RealmDTO
            realms = searchRealmCmd.realms
            If realms.Length = 0 Then Throw New System.Exception("Could not find realm " + SecurityDomain)


            domain = realms(0).topLevelSecurityDomain
            idSource = realms(0).identitySources(0)

            Dim cmd As New SearchPrincipalsIterativeCommand
            cmd.limit = 1000
            cmd.identitySourceGuid = idSource.guid
            cmd.filter = Filter.equal(PrincipalDTO._LOGINUID, AccountName)
            Dim results() As PrincipalDTO
            cmd.execute()
            results = cmd.principals
            If results.Length = 0 Then Throw New System.Exception("Unable to locate user " + AccountName)
            For i As Integer = 0 To (results.Length - 1)
                Dim nw As DataRow = dt.NewRow
                nw("guid") = results(i).guid
                nw("userid") = results(i).userID
                nw("first") = results(i).firstName
                nw("last") = results(i).lastName
                nw("middle") = results(i).middleName
                nw("email") = results(i).email
                nw("passwordexpired") = results(i).passwordExpired
                nw("certificateDN") = results(i).certificateDN
                nw("accountstart") = results(i).accountStartDate.ToString
                nw("accountexpire") = results(i).accountExpireDate.ToString
                nw("domainGuid") = results(i).securityDomainGuid
                nw("identitySourceGuid") = results(i).identitySourceGuid
                nw("canBeImpersonated") = results(i).canBeImpersonated
                nw("trustToImpersonate") = results(i).trustToImpersonate
                nw("lockoutStatus") = results(i).lockoutStatus
                nw("emergency lockout status") = results(i).emergencyLockoutStatus
                dt.Rows.Add(nw)
            Next
            Return GenerateActivityResult(dt)



        End Function



        Function ValidateServerCertificate(ByVal sender As Object, ByVal certificate As X509Certificate,
  ByVal chain As X509Chain, ByVal sslPolicyErrors As SslPolicyErrors) As Boolean
            Return True
        End Function

    End Class
End Namespace