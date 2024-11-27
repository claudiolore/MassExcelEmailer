﻿using System;
using System.IO;
using OfficeOpenXml;
using System.Net.Mail;
using System.Net.Mime;
using System.Collections.Generic;
using email_sender;
using MimeKit;
using MimeKit.Text;
using MailKit.Security;
using MailKit.Net.Smtp;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace ExcelEmailSender
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                string subject = string.Empty;
                string body = string.Empty;
                string excelPath = string.Empty;
                string outputPdfPath = string.Empty;
                string pdfPath = string.Empty;
                int emailInviate = 0;
                int emailFallite = 0;
                string risposta;



                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n\t\t\t\t╔═══════════════════════╗");
                Console.WriteLine("\t\t\t\t║   Excel Email Sender  ║");
                Console.WriteLine("\t\t\t\t║      BENVENUTO        ║");
                Console.WriteLine("\t\t\t\t╚═══════════════════════╝\n");
                Console.ResetColor();

                //VALIDAZIONE CREDENZIALI
                var credentialValidator = new CredentialValidator();
                var (senderEmail, senderPassword) = credentialValidator.ValidateCredentials();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("--Inserisci il percorso del file Excel in cui ci sono le email a cui inviare: ");
                Console.ResetColor();
                excelPath = Console.ReadLine().Trim('"');

                if (string.IsNullOrEmpty(excelPath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n⚠ ATTENZIONE! Devi inserire almeno un carattere!");
                    Console.WriteLine("Premi qualunque tasto per riprovare");
                    Console.ResetColor();
                    Console.ReadKey();
                    continue;
                }

                if (!File.Exists(excelPath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n⚠ ATTENZIONE! Il file Excel specificato non esiste o il percorso è sbagliato!");
                    Console.WriteLine("Premi qualunque tasto per riprovare");
                    Console.ResetColor();
                    Console.ReadKey();
                    continue;
                }

                //CARICAMENTO INFORMAZIONI AZIENDE
                List<Azienda> listaAziende = ReadAziendeFromExcel(excelPath);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nLettura del file: {excelPath}\n");
                Console.ResetColor();

                List<string> emailAddresses = GetEmails(listaAziende);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\nTrovate {emailAddresses.Count} email da inviare.");
                Console.ResetColor();

                if (emailAddresses.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n⚠ ATTENZIONE! Nessuna email trovata nel file Excel.");
                    Console.ResetColor();
                    Console.ReadKey();
                    continue;
                }

                //INSERIMENTO OGGETTO EMAIL
                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("\n--Inserisci l'oggetto dell'email: ");
                    Console.ResetColor();
                    subject = Console.ReadLine();

                    if (string.IsNullOrEmpty(subject))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n⚠ ATTENZIONE! Inserire almeno un carattere");
                        Console.WriteLine("Premi un tasto qualunque per riprovare");
                        Console.ResetColor();
                        Console.ReadKey();
                        continue;
                    }
                    break;
                }

                //INSERIMENTO CORPO EMAIL
                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("\n--Inserisci il corpo dell'email: ");
                    Console.ResetColor();
                    body = Console.ReadLine();

                    if (string.IsNullOrEmpty(body))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n⚠ ATTENZIONE! Inserire almeno un carattere");
                        Console.WriteLine("Premi un tasto qualunque per riprovare");
                        Console.ResetColor();
                        Console.ReadKey();
                        continue;
                    }
                    break;
                }

                //ALLEGARE UN PDF
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("\nVuoi allegare un PDF? (S/N): ");
                Console.ResetColor();
                bool attachPdf = Console.ReadLine().ToUpper() == "S";


                if (attachPdf)
                {
                    while (true)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("\nInserisci il percorso del file PDF (deve avere una colonna nominativo, email e indirizzo: ");
                        Console.ResetColor();
                        pdfPath = Console.ReadLine().Trim('"');

                        if (!File.Exists(pdfPath))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("\n⚠ ATTENZIONE! Il file PDF specificato non esiste o il percorso è sbagliato!");
                            Console.WriteLine("Premi qualunque tasto per riprovare");
                            Console.ResetColor();
                            Console.ReadKey();
                            continue;
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\n[✓] File PDF trovato: {pdfPath}");
                        Console.ResetColor();
                        break;
                    }
                }


                PrintResoconto(senderEmail, excelPath, pdfPath, subject, body);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nVuoi continuare? (s/n)");
                Console.ResetColor();
                
                risposta = string.Empty;
                risposta = Console.ReadLine().ToLower();

                if (!risposta.Equals("s"))
                {
                    break;
                }


                //INVIO EMAIL
                foreach (string email in emailAddresses)
                {
                    try
                    {
                        string pdfDaModificare = pdfPath;
                        Azienda azienda = listaAziende.FirstOrDefault(a => a.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                        if (azienda == null)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"\n[⚠] Nessuna azienda trovata per l'email: {email}");
                            Console.ResetColor();
                            continue;
                        }

                        string name = azienda.Nome;
                        string address = azienda.Indirizzo;

                        if(attachPdf)
                        {
                            PdfFormFiller.FillPdf(pdfDaModificare, outputPdfPath, name, address, email);
                        }

                        SendEmail(senderEmail, senderPassword, email, subject, body, attachPdf ? outputPdfPath : null);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\n[✓] Email inviata con successo a: {email}");
                        Console.ResetColor();
                        emailInviate++;
                    }
                    catch (SmtpException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n[✗] SMTP Error: {ex.StatusCode} - {ex.Message}");
                        Console.ResetColor();
                        emailFallite++;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n[✗] Errore nell'invio dell'email a {email}: {ex.Message}");
                        Console.ResetColor();
                        emailFallite++;
                    }
                }

                PrintFinalReport(emailInviate, emailFallite);
                risposta = Console.ReadLine().ToLower();

                if (risposta == "e")
                {
                    break;
                }
            }

        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------
        static List<string> ReadEmailsFromExcel(string filePath)
        {
            List<string> emails = new List<string>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                if (package.Workbook.Worksheets.Count == 0)
                {
                    Console.WriteLine("\nATTENZIONE!!! Il file Excel non contiene fogli di lavoro!");
                    return emails;
                }

                var worksheet = package.Workbook.Worksheets[0];

                // Verifica se il foglio contiene righe
                if (worksheet.Dimension == null)
                {
                    Console.WriteLine("\nATTENZIONE!!! Il foglio di lavoro è vuoto!");
                    return emails;
                }

                int rowCount = worksheet.Dimension.Rows;
                int colCount = worksheet.Dimension.Columns;

                // Cerca la colonna "Email" nella prima riga
                int emailColumnIndex = -1;
                for (int col = 1; col <= colCount; col++)
                {
                    string header = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(header) && header.Equals("Email", StringComparison.OrdinalIgnoreCase))
                    {
                        emailColumnIndex = col;
                        break;
                    }
                }

                if (emailColumnIndex == -1)
                {
                    Console.WriteLine("\nATTENZIONE!!! Non è stata trovata alcuna colonna 'Email' nella prima riga!");
                    return emails;
                }

                Console.WriteLine($"\nColonna 'Email' trovata all'indice: {emailColumnIndex}");

                // Leggi le email dalla colonna identificata
                for (int row = 2; row <= rowCount; row++) // Dalla seconda riga in poi (saltando l'intestazione)
                {
                    string email = worksheet.Cells[row, emailColumnIndex].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(email))
                    {
                        emails.Add(email);
                        Console.WriteLine($"-Email trovata: {email}");
                    }
                }
            }

            return emails;
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------
        public static List<Azienda> ReadAziendeFromExcel(string filePath)
        {
            List<Azienda> aziende = new List<Azienda>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                if (package.Workbook.Worksheets.Count == 0)
                {
                    Console.WriteLine("\nATTENZIONE!!! Il file Excel non contiene fogli di lavoro!");
                    return aziende;
                }

                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet.Dimension == null)
                {
                    Console.WriteLine("\nATTENZIONE!!! Il foglio di lavoro è vuoto!");
                    return aziende;
                }

                int rowCount = worksheet.Dimension.Rows;
                int colCount = worksheet.Dimension.Columns;

                int nomeColumnIndex = -1;
                int indirizzoColumnIndex = -1;
                int emailColumnIndex = -1;

                // Identifica le colonne necessarie
                for (int col = 1; col <= colCount; col++)
                {
                    string header = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(header))
                    {
                        if (header.Equals("Nominativo", StringComparison.OrdinalIgnoreCase))
                            nomeColumnIndex = col;
                        else if (header.Equals("Indirizzo", StringComparison.OrdinalIgnoreCase))
                            indirizzoColumnIndex = col;
                        else if (header.Equals("Email", StringComparison.OrdinalIgnoreCase))
                            emailColumnIndex = col;
                    }
                }

                if (nomeColumnIndex == -1 || indirizzoColumnIndex == -1 || emailColumnIndex == -1)
                {
                    Console.WriteLine("\nATTENZIONE!!! Non tutte le colonne richieste ('Nome', 'Indirizzo', 'Email') sono presenti!");
                    return aziende;
                }

                // Leggi i dati riga per riga
                for (int row = 2; row <= rowCount; row++)
                {
                    string nome = worksheet.Cells[row, nomeColumnIndex].Value?.ToString()?.Trim();
                    string indirizzo = worksheet.Cells[row, indirizzoColumnIndex].Value?.ToString()?.Trim();
                    string email = worksheet.Cells[row, emailColumnIndex].Value?.ToString()?.Trim();

                    if (!string.IsNullOrEmpty(email))
                    {
                        aziende.Add(new Azienda
                        {
                            Id = Guid.NewGuid(),
                            Nome = nome,
                            Indirizzo = indirizzo,
                            Email = email
                        });
                    }
                }
            }

            return aziende;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------
        static void SendEmail(string senderEmail, string senderPassword, string recipientEmail,
                            string subject, string body, string pdfPath = null)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("", senderEmail));
                message.To.Add(new MailboxAddress("", recipientEmail));
                message.Subject = subject;

                var builder = new BodyBuilder();
                builder.TextBody = body;

                // Aggiungi PDF se specificato
                if (!string.IsNullOrEmpty(pdfPath))
                {
                    builder.Attachments.Add(pdfPath);
                }

                message.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // Disabilita la verifica del certificato SSL (solo per debug)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    // Connessione con timeout esteso
                    //client.Connect("smtps.aruba.it", 465, SecureSocketOptions.SslOnConnect);
                    //client.Connect("smtp.gmail.it", 587, SecureSocketOptions.SslOnConnect);
                    client.Connect("localhost", 1025);

                    // Autenticazione
                    client.Authenticate(senderEmail, senderPassword);

                    // Timeout più lungo per l'invio
                    client.Timeout = 20000; // 30 secondi

                    // Invio
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERRORE DETTAGLIATO] Invio email fallito:");
                Console.WriteLine($"Messaggio: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------
        public static List<string> GetEmails(List<Azienda> aziende)
        {
            return aziende.Select(a => a.Email).ToList();
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------
        public static void PrintResoconto(string senderEmail, string excelPath, string pdfPath, string subject, string body)
        {
            // RESOCONTO
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n\t===== RESOCONTO =====");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Email mittente: ");
            Console.ResetColor();
            Console.WriteLine(senderEmail);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Path Excel: ");
            Console.ResetColor();
            Console.WriteLine(excelPath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Path PDF: ");
            Console.ResetColor();
            Console.WriteLine(pdfPath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Oggetto email: ");
            Console.ResetColor();
            Console.WriteLine(subject);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Body: ");
            Console.ResetColor();
            string shortBody = body.Length > 200 ? body.Substring(0, 200).TrimEnd() : body.TrimEnd();
            Console.WriteLine($"{shortBody}...");
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------
        public static void PrintFinalReport(int emailInviate, int emailFallite)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n\t===== REPORT FINALE =====");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nEmail inviate {emailInviate}");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nEmail fallite {emailFallite}");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nProcesso completato.");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Premi 'E' per uscire.");
            Console.WriteLine("Oppure un altro tasto per ripetere.");
            Console.ResetColor();
        }

    }
}
