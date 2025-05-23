# AvenderLine - Qatari Abaya E-Commerce Platform

![AvenderLine Logo](LavenderLine/media/avenderline.png) 

## Project Overview
A sophisticated e-commerce platform specializing in traditional Qatari abayas, built with ASP.NET MVC and modern web technologies. Designed to showcase cultural elegance while providing a seamless shopping experience.

**Video Demo**  
[![Demo Video](LavenderLine/media/demo-preview.png)](https://drive.google.com/file/d/1d-GRqKu6O5isv5qzY6csyF522g6HUfH3/view?usp=sharing)

## Key Features
- **Cultural Authentication**  
  - Qatari-themed UI with Arabic/English support
  - Google & ASP.NET Identity integration
- **Abaya Commerce**
  - Product catalog with size/length customization
  - High-resolution image gallery
  - Inventory management
- **Payment Integration**
  - MyFatoorah payment gateway (QAR transactions)
  - SSL secure checkout
- **Qatari Cloud Services**
  - Google Cloud Storage for media
  - SendGrid Arabic/English notifications
- **Admin Portal**
  - Order management dashboard
  - Payment management dashboard
  - Cultural event promotions
  - Analytics integration

## Technology Stack
| Category          | Technologies                                                                 |
|-------------------|------------------------------------------------------------------------------|
| **Backend**       | ASP.NET MVC, EF Core, PostgreSQL, Identity Framework                        |
| **Frontend**      | Bootstrap 5, jQuery, AJAX, Owl Carousel, noUiSlider                         |
| **APIs**          | Google OAuth, MyFatoorah Payment API, SendGrid, Google Cloud Storage        |
| **Security**      | ASP.NET Core Identity, AntiForgery Tokens, User Secrets                      |
| **DevOps**        | Docker, GitHub Actions, PostgreSQL migrations                               |

## Installation
```bash
# Clone repository
git clone https://github.com/yourusername/AvenderLine.git

# Restore dependencies
dotnet restore

# Database setup
dotnet ef database update
