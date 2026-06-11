import { Component } from '@angular/core';
import { ProductModel } from './productmodel';
import {CommonModule} from '@angular/common';
@Component({
  selector: 'app-product',
  imports: [CommonModule],
  templateUrl: './product.html',
  styleUrl: './product.css',
})
export class Product {
products: ProductModel[] = [
  new ProductModel("Samsung Galaxy S21", 20000, "Latest Samsung smartphone with advanced features.", "https://miro.medium.com/1*53xRFGSOhc1RS1xlDN_Ixw.jpeg"),
  new ProductModel("Apple iPhone 13", 30000, "Latest Apple iPhone with powerful performance and sleek design.", "https://store.storeimages.cdn-apple.com/4982/as-images.apple.com/is/iphone-13-pro-max-silver-select?wid=940&hei=1112&fmt=png-alpha&.v=1645552346275"),
  new ProductModel("Google Pixel 6", 25000, "Google's flagship smartphone with excellent camera capabilities.", "https://media-ik.croma.com/prod/https://media.tatacroma.com/Croma%20Assets/Communication/Mobiles/Images/318397_0_mZAYq7-9W0.png?updatedAt=1755701218577"),
  new ProductModel("OnePlus 9 Pro", 28000, "High-performance smartphone with fast charging and smooth display.", "https://image01.oneplus.net/media/202511/05/28171b4e0509fc613b1e314a609941c0.png?x-amz-process=image/format,webp/quality,Q_80"),
  new ProductModel("Xiaomi Mi 11", 22000, "Affordable smartphone with powerful features and sleek design.", "https://pbs.twimg.com/media/HJZdcdPW4AEp-bU.jpg"),
];
}
