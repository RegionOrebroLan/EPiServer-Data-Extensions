<%@ Page Language="C#" AutoEventWireup="false" CodeBehind="StartPageTemplate.aspx.cs" Inherits="Application.Views.Pages.StartPageTemplate" %>
<!DOCTYPE html>
<html lang="<%= this.CurrentPage.Language %>">
	<head>
		<meta charset="utf-8" />
		<meta name="viewport" content="width=device-width, initial-scale=1.0" />
		<title><%= this.CurrentPage.Name %></title>
	</head>
	<body>
		<h1><%= this.CurrentPage.Name %></h1>
		<ul>
			<li><a href="/">/</a></li>
			<li><a href="/en">/en</a></li>
			<li><a href="/sv">/sv</a></li>
			<li><a href="/EPiServer/CMS">/EPiServer/CMS</a></li>
		</ul>
	</body>
</html>