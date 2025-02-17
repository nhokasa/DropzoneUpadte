import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Select2OptionData } from 'ng-select2';
import { SnotifyPosition } from 'ng-snotify';
import { takeUntil } from 'rxjs/operators';
import { Product } from 'src/app/_core/_models/product';
import { AlertUtilityService } from 'src/app/_core/_services/alert-utility.service';
import { DestroyService } from 'src/app/_core/_services/destroy.service';
import { ProductCategoryService } from 'src/app/_core/_services/product-category.service';
import { ProductService } from 'src/app/_core/_services/product.service';
import { SignalRService } from 'src/app/_core/_services/signal-r.service';
import { Pagination, PaginationResult } from 'src/app/_core/_utility/pagination';

@Component({
  selector: 'app-list',
  templateUrl: './list.component.html',
  styleUrls: ['./list.component.scss'],
  providers: [DestroyService]
})
export class ListComponent implements OnInit {
  productAll: Product[];
  products: Product[];
  product: any = {};
  text: string = '';
  flag: string = '0';
  pagination: Pagination;
  productCateList: Array<Select2OptionData>;
  productCateID: string = 'all';
  productList: Array<Select2OptionData>;
  product_Name: string = 'all';
  listProduct: Product[] = [];
  checkboxAll: boolean = false;
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private productService: ProductService,
    private productCategoryService: ProductCategoryService,
    private alertUtility: AlertUtilityService,
    private signalRService: SignalRService,
    private destroyService: DestroyService,
  ) { }

  ngOnInit() {
    this.route.data.pipe(takeUntil(this.destroyService.destroys$))
      .pipe(takeUntil(this.destroyService.destroys$))
      .subscribe(data => {
        this.productAll = data['products'].result;
        this.pagination = data['products'].pagination;
        this.products = this.productAll.slice((this.pagination.currentPage - 1) * this.pagination.pageSize, this.pagination.pageSize * this.pagination.currentPage);
      });
    this.signalRService.startConnection();
    if (this.signalRService.hubConnection) {
      this.signalRService.hubConnection.on('LoadDataProduct', () => {
        this.getDataPaginations();
      });
    }
    this.getProductCateList();
  }

  addNew() {
    this.productService.changeProduct(this.product);
    this.productService.changeFlag('0');
    this.router.navigate(['/product/add']);
  }

  edit(product: Product) {
    this.productService.changeProduct(product);
    this.productService.changeFlag('1');
    this.router.navigate(['/product/edit']);
  }

  remove(product: Product) {
    this.listProduct.push(product);
    this.checkDelete(this.listProduct, 'Are you sure delete product?');
  }

  removeMulti() {
    const productList = this.productAll.filter(item => {
      return item.checked === true;
    });
    if (productList.length === 0) {
      return this.alertUtility.error('Error', 'Please choose item to delete');
    }
    this.checkDelete(productList, "Are you sure delete " + productList.length + " items?");
  }

  checkDelete(products: Product[], alert: string) {
    this.alertUtility.confirmDelete(alert, SnotifyPosition.rightCenter, () => {
      this.productService.remove(products)
        .pipe(takeUntil(this.destroyService.destroys$))
        .subscribe(res => {
          if (res.success) {
            this.alertUtility.success('Success!', res.message);
            this.listProduct = [];
            this.text = '';
            this.getDataPaginations();
          }
          else {
            this.alertUtility.error('Error!', res.message);
          }
        },
          error => {
            console.log(error);
          });
    });
  }

  getDataPaginations() {
    this.productService.getDataPaginations(this.pagination.currentPage, this.pagination.pageSize, this.text)
      .pipe(takeUntil(this.destroyService.destroys$))
      .subscribe((res: PaginationResult<Product>) => {
        this.productAll = res.result;
        this.pagination = res.pagination;
        this.products = this.productAll.slice((this.pagination.currentPage - 1) * this.pagination.pageSize, this.pagination.pageSize * this.pagination.currentPage);
        this.checkboxAll = false;
      }), error => {
        this.alertUtility.error('Error!', error);
      };
  }

  searchDataPaginations() {
    this.productService.searchDataPaginations(this.pagination.currentPage, this.pagination.pageSize, this.productCateID, this.product_Name)
      .pipe(takeUntil(this.destroyService.destroys$))
      .subscribe((res: PaginationResult<Product>) => {
        this.productAll = res.result;
        this.pagination = res.pagination;
        this.products = this.productAll.slice((this.pagination.currentPage - 1) * this.pagination.pageSize, this.pagination.pageSize * this.pagination.currentPage);
        this.checkboxAll = false;
      }), error => {
        this.alertUtility.error('Error!', error);
      };
  }

  changeNew(product: Product) {
    this.productService.changeNew(product)
      .pipe(takeUntil(this.destroyService.destroys$))
      .subscribe(res => {
        if (res.success) {
          this.alertUtility.success('Success!', res.message);
          this.getDataPaginations();
        } else {
          this.alertUtility.error('Error!', res.message);
        }
      },
        error => {
          console.log(error);
        }
      );
  }

  changeHotSale(product: Product) {
    this.productService.changeHotSale(product)
      .pipe(takeUntil(this.destroyService.destroys$))
      .subscribe(res => {
        if (res.success) {
          this.alertUtility.success('Success!', res.message);
          this.getDataPaginations();
        } else {
          this.alertUtility.error('Error!', res.message);
        }
      },
        error => {
          console.log(error);
        }
      );
  }

  changeIsSale(product: Product) {
    this.productService.changeIsSale(product)
      .pipe(takeUntil(this.destroyService.destroys$))
      .subscribe(res => {
        if (res.success) {
          this.alertUtility.success('Success!', res.message);
          this.getDataPaginations();
        } else {
          this.alertUtility.error('Error!', res.message);
        }
      },
        error => {
          console.log(error);
        }
      );
  }

  changeStatus(product: Product) {
    this.productService.changeStatus(product)
      .pipe(takeUntil(this.destroyService.destroys$))
      .subscribe(res => {
        if (res.success) {
          this.alertUtility.success('Success!', res.message);
          this.getDataPaginations();
        } else {
          this.alertUtility.error('Error!', res.message);
        }
      },
        error => {
          console.log(error);
        }
      );
  }

  pageChanged(event: any): void {
    this.pagination.currentPage = event.page;
    this.products = this.productAll.slice((this.pagination.currentPage - 1) * this.pagination.pageSize, this.pagination.pageSize * this.pagination.currentPage);
  }

  getProductCateList() {
    this.productCategoryService.getIdAndName()
      .pipe(takeUntil(this.destroyService.destroys$))
      .subscribe(res => {
        this.productCateList = res.map(item => {
          return { id: item.id, text: item.name };
        });
        this.productCateList.unshift({ id: 'all', text: 'All' });
      });
  }

  getProductListByProductCateID() {
    this.productService.getProductListByProductCateID(this.productCateID)
      .pipe(takeUntil(this.destroyService.destroys$))
      .subscribe(res => {
        this.productList = res.map(item => {
          return { id: item.id, text: item.name };
        });
        this.productList.unshift({ id: 'all', text: 'All' });
      });
  }

  changeProductCateID(event) {
    this.productCateID = event;
    this.getProductListByProductCateID();
  }

  exportExcel(checkExport: number) {
    let checkSearch = this.productCateID === 'all' ? 1 : 2;
    if (this.productAll.length > 0 && this.products.length > 0)
      return this.productService.exportListAspose(this.pagination.currentPage, this.pagination.pageSize, this.text, checkExport, this.productCateID, this.product_Name, checkSearch);
    else
      return this.alertUtility.warning('Warning', 'No data');
  }

  checkAll(e) {
    if (e.target.checked) {
      this.productAll.forEach(element => {
        element.checked = true;
      });
    }
    else {
      this.productAll.forEach(element => {
        element.checked = false;
      });
    }
  }

  checkElement() {
    let countProductCateCheckBox = this.productAll.filter(x => x.checked !== true).length;
    if (countProductCateCheckBox === 0) {
      this.checkboxAll = true;
    } else {
      this.checkboxAll = false;
    }
  }

  pdf() {
    debugger
    return this.productService.pdf();
  }
}
