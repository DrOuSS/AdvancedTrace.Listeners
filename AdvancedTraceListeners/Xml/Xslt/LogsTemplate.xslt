<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:xs="http://www.w3.org/2001/XMLSchema"
                exclude-result-prefixes="xs">

  <xsl:output method="html" version="4.0"/>
  <xsl:key name="category" match="Logs/Log" use="@UserCategory"/>
  <xsl:template match="/">
    <html>
      <head>
        <title>Logs</title>
        <style type="text/css">
          .sevInfo {background-image:url('info.png');background-repeat:no-repeat;text-align:center;}
          .sevError {background-image:url('problem.png');background-repeat:no-repeat;text-align:center;}
          .sevWarning {background-image:url('warning.png');background-repeat:no-repeat;text-align:center;}
          .sevFatal {background-image:url('fatal.png');background-repeat:no-repeat;text-align:center;}
          .sevSql {background-image:url('sql.png');background-repeat:no-repeat;text-align:center;}
          .sevDatabase {background-image:url('database.png');background-repeat:no-repeat;text-align:center;}
          .sevBug {background-image:url('bug.png');background-repeat:no-repeat;text-align:center;}
        </style>
      </head>
      <body>
        <h1>
          <center>
            <font color="#4f81bd">
              <xsl:value-of select="Logs/@application_name"/>
              (<xsl:value-of select="Logs/@filename"/>)
            </font>
          </center>
        </h1>
        <div align="center">
          <xsl:for-each
            select="Logs/Log[generate-id()=generate-id(key('category',@UserCategory))]">
            <xsl:variable name="strCategory" select="@UserCategory"/>
            <xsl:variable name="lstLog" select="//Logs/Log[@UserCategory=$strCategory]"/>
            <table cellpadding="0" cellspacing="0" width="100%">
              <TH bgcolor="#4f81bd" align="left" valign="bottom"  height="50">
                <H3>
                  <font color="#FFFFFF">
                    Category:
                    <xsl:value-of select="$strCategory"/>
                  </font>
                </H3>
              </TH>
              <TR>
                <table cellpadding="0" cellspacing="0" width="100%" border="0">
                  <TH bgcolor="#95b3d7" width="150" height="30">
                    <font color="#FFFFFF">Top</font>
                  </TH>
                  <TH bgcolor="#95b3d7" width="30">
                    <font color="#FFFFFF">Sev</font>
                  </TH>
                  <TH bgcolor="#95b3d7">
                    <font color="#FFFFFF">Message</font>
                  </TH>
                  <xsl:for-each select="$lstLog">
                    <xsl:variable name="bgColor">
                      <xsl:choose>
                        <xsl:when test="position() mod 2 = 1">#dbe5f1</xsl:when>
                        <xsl:otherwise></xsl:otherwise>
                      </xsl:choose>
                    </xsl:variable>
                    <TR bgcolor="{$bgColor}">
                      <TD>

                        <xsl:value-of select="@Time"/>
                        <!--
                        <xsl:variable name="date" as="xs:dateTime" select="@Time"/>
                        <xsl:value-of select="$date"/>
                        <xsl:value-of select="xs:format-dateTime($date, '[D01]/[M01]/[Y0001]')" />
                        <xsl:value-of select="adjust-dateTime-to-timezone(@Time, '[D01]/[M01]/[Y0001]')"/>
                        
                        <xsl:value-of select="format-dateTime(@Time, '[D] [MNn] [Y] [h]:[m01][PN,*-2] [ZN,*-3]', ('fr'), (), 'fr')"/>
                        <xsl:value-of select="concat(ms:format-date(@Time, 'dd/MM/yyyy'), ms:format-time(@Time, ' HH:mm:ss'))"/>
                        <xsl:value-of select="@Time"/>-->
                      </TD>
                      <xsl:choose>
                        <xsl:when test="@Severity='__ADVANCED_TRACE_INFORMATION__' or @Severity='Information'">
                          <td class="sevInfo"/>
                        </xsl:when>
                        <xsl:when test="@Severity='__ADVANCED_TRACE_ERROR__' or @Severity='Problem'">
                          <td class="sevError"/>
                        </xsl:when>
                        <xsl:when test="@Severity='__ADVANCED_TRACE_WARNING__' or @Severity='Warning'">
                          <td class="sevWarning"/>
                        </xsl:when>
                        <xsl:when test="@Severity='__ADVANCED_TRACE_FATAL__' or @Severity='Fatal'">
                          <td class="sevFatal"/>
                        </xsl:when>
                        <xsl:when test="@Severity='__ADVANCED_TRACE_SQL__' or @Severity='Sql'">
                          <td class="sevSql"/>
                        </xsl:when>
                        <xsl:when test="@Severity='__ADVANCED_TRACE_DATABASE__' or @Severity='Database'">
                          <td class="sevDatabase"/>
                        </xsl:when>
                        <xsl:when test="@Severity='__ADVANCED_TRACE_DEBUG__' or @Severity='Bug'">
                          <td class="sevBug"/>
                        </xsl:when>
                      </xsl:choose>
                      <TD>
                        <xsl:choose>
                          <xsl:when test="Exception/@Message">
                            <xsl:call-template name="Exceptions"/>
                          </xsl:when>
                          <xsl:otherwise>
                            <xsl:value-of select="@Message"/>
                          </xsl:otherwise>
                        </xsl:choose>
                      </TD>
                    </TR>
                  </xsl:for-each>
                </table>
              </TR>
            </table>
            <BR/>
            <BR/>
            <BR/>
          </xsl:for-each>
        </div>
      </body>
    </html>
  </xsl:template>

  <xsl:template name="Exceptions">
    <ul>
      <li >
        <font color="#FF0000">
          <xsl:value-of select="@Message"/>
        </font>
      </li>
      <xsl:call-template name="ExceptionDetail"/>
    </ul>
  </xsl:template>

  <xsl:template name="ExceptionDetail">
    <li>
      <font color="#FF0000">
        <xsl:value-of select="Exception/@Message"/>
      </font>
    </li>
    <xsl:if test="Exception/@Source">
      <li>
        <B>Source:</B>
        <I>
          <xsl:value-of select="Exception/@Source"/>
        </I>
      </li>
    </xsl:if>
    <xsl:if test="Exception/@StackTrace">
      <li>
        <B>Stack trace:</B>
        <I>
          <xsl:value-of select="Exception/@StackTrace"/>
        </I>
      </li>
    </xsl:if>
    <xsl:for-each select="child::*">
      <xsl:choose>
        <xsl:when test="Exception/@Message">
          <ul>
            <xsl:call-template name="ExceptionDetail"/>
          </ul>
        </xsl:when>
      </xsl:choose>
    </xsl:for-each>
  </xsl:template>

</xsl:stylesheet>


